using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.HangedMan;

/// <summary>
/// Handles the "Висельница" suicide mechanic: dragging a mob onto the structure
/// equips a noose cloak that immobilizes the wearer and asphyxiates them.
/// </summary>
public sealed partial class SharedHangedManSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private const string NeckSlot = "neck";

    public override void Initialize()
    {
        base.Initialize();

        // Structure: drag a mob onto it to hang them.
        SubscribeLocalEvent<HangedManStructureComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<HangedManStructureComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<HangedManStructureComponent, HangDoAfterEvent>(OnHangDoAfter);

        // Cloak: equip/unequip wiring.
        SubscribeLocalEvent<HangedManComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HangedManComponent, GotUnequippedEvent>(OnGotUnequipped);

        // Victim: blocks, height, damage. Removal goes through the normal
        // clothing unequip (self) and strip (others) systems.
        SubscribeLocalEvent<HangedManVictimComponent, ComponentStartup>(OnVictimStartup);
        SubscribeLocalEvent<HangedManVictimComponent, ComponentShutdown>(OnVictimShutdown);
        SubscribeLocalEvent<HangedManVictimComponent, UpdateCanMoveEvent>(OnVictimUpdateCanMove);
        SubscribeLocalEvent<HangedManVictimComponent, BeingPulledAttemptEvent>(OnVictimBeingPulled);
        SubscribeLocalEvent<HangedManVictimComponent, BuckleAttemptEvent>(OnVictimBuckleAttempt);
        SubscribeLocalEvent<HangedManVictimComponent, ContainerGettingInsertedAttemptEvent>(OnVictimInsertAttempt);
    }

    #region Structure

    private void OnCanDropTarget(Entity<HangedManStructureComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanHang(args.Dragged);
        args.Handled = true;
    }

    private void OnDragDropTarget(Entity<HangedManStructureComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || !CanHang(args.Dragged))
            return;

        args.Handled = true;

        if (args.User == args.Dragged)
            _popup.PopupPredicted(Loc.GetString("hangedman-begin-hang-self"), ent, args.User, PopupType.LargeCaution);
        else
            _popup.PopupPredicted(
                Loc.GetString("hangedman-begin-hang-self-other", ("target", Identity.Entity(args.Dragged, EntityManager))),
                Loc.GetString("hangedman-begin-hang-other",
                    ("user", Identity.Entity(args.User, EntityManager)),
                    ("target", Identity.Entity(args.Dragged, EntityManager))),
                ent, args.User, PopupType.LargeCaution);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HangDelay,
            new HangDoAfterEvent(),
            ent,
            target: args.Dragged)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnHangDoAfter(Entity<HangedManStructureComponent> ent, ref HangDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } victim || !CanHang(victim))
            return;

        args.Handled = true;

        // Mutations are server authoritative.
        if (_net.IsClient)
            return;

        // Knock the existing neck item to the floor if the slot is occupied.
        if (_inventory.TryGetSlotEntity(victim, NeckSlot, out _))
            _inventory.TryUnequip(victim, NeckSlot, out _, force: true, reparent: true);

        // Hang the victim on the structure's tile, not on their own.
        var structureCoords = Transform(ent).Coordinates;
        _transform.SetCoordinates(victim, structureCoords);

        var cloak = Spawn(ent.Comp.Cloak, structureCoords);

        if (!_inventory.TryEquip(victim, cloak, NeckSlot, silent: true, force: true))
        {
            // Couldn't equip - bail out and leave the cloak on the floor.
            _transform.SetCoordinates(cloak, Transform(victim).Coordinates);
            return;
        }

        QueueDel(ent);
    }

    #endregion

    #region Cloak

    private void OnGotEquipped(Entity<HangedManComponent> ent, ref GotEquippedEvent args)
    {
        // Server authoritative: the victim component is networked to the client.
        if (args.Slot != NeckSlot || _net.IsClient)
            return;

        var victim = EnsureComp<HangedManVictimComponent>(args.EquipTarget);
        victim.Cloak = ent.Owner;
        victim.NextDamage = _timing.CurTime + victim.DamageInterval;
        Dirty(args.EquipTarget, victim);

        // Cable ties tightening as the victim is hung.
        _audio.PlayPvs(ent.Comp.HangSound, args.EquipTarget);

        _popup.PopupPredicted(Loc.GetString("hangedman-hanged-self"),
            Loc.GetString("hangedman-hanged-other", ("target", Identity.Entity(args.EquipTarget, EntityManager))),
            args.EquipTarget, args.EquipTarget, PopupType.LargeCaution);
    }

    private void OnGotUnequipped(Entity<HangedManComponent> ent, ref GotUnequippedEvent args)
    {
        // Server authoritative: deletion and component removal are replicated to the client.
        if (args.Slot != NeckSlot || _net.IsClient)
            return;

        if (HasComp<HangedManVictimComponent>(args.EquipTarget))
            RemComp<HangedManVictimComponent>(args.EquipTarget);

        // The noose vanishes once removed.
        QueueDel(ent);
    }

    #endregion

    #region Victim

    private void OnVictimStartup(Entity<HangedManVictimComponent> ent, ref ComponentStartup args)
    {
        _actionBlocker.UpdateCanMove(ent);

        if (!_net.IsServer)
            return;

        // Anchor so the victim cannot be shoved around by lockers, other mobs, etc.
        _transform.AnchorEntity(ent.Owner);
    }

    private void OnVictimShutdown(Entity<HangedManVictimComponent> ent, ref ComponentShutdown args)
    {
        _actionBlocker.UpdateCanMove(ent);

        if (!_net.IsServer)
            return;

        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (Transform(ent.Owner).Anchored)
            _transform.Unanchor(ent.Owner);

        // Drop to the ground once released from the noose.
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(3), refresh: true);
    }

    private void OnVictimUpdateCanMove(Entity<HangedManVictimComponent> ent, ref UpdateCanMoveEvent args)
    {
        // Don't block while the component is being removed, otherwise the
        // re-evaluation during shutdown leaves CanMove cached as false forever.
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnVictimBeingPulled(Entity<HangedManVictimComponent> ent, ref BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }

    // Can't sit a hanged victim down on a chair.
    private void OnVictimBuckleAttempt(Entity<HangedManVictimComponent> ent, ref BuckleAttemptEvent args)
    {
        args.Cancelled = true;
    }

    // Can't stuff a hanged victim into a trash bin, locker, etc.
    private void OnVictimInsertAttempt(Entity<HangedManVictimComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Forcibly removes the noose from a victim. Unequipping it triggers
    /// <see cref="OnGotUnequipped"/>, which cleans up the victim state and deletes it.
    /// Server authoritative.
    /// </summary>
    public void RemoveNoose(Entity<HangedManVictimComponent> ent)
    {
        if (_net.IsClient)
            return;

        _inventory.TryUnequip(ent, NeckSlot, out _, force: true, reparent: true);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<HangedManVictimComponent>();
        while (query.MoveNext(out var uid, out var victim))
        {
            if (_timing.CurTime < victim.NextDamage)
                continue;

            victim.NextDamage += victim.DamageInterval;
            Dirty(uid, victim);

            if (_mobState.IsDead(uid))
                continue;

            _damageable.TryChangeDamage(uid, victim.Damage, ignoreResistances: true);
        }
    }

    private bool CanHang(EntityUid target)
    {
        return !HasComp<HangedManVictimComponent>(target)
            && HasComp<InventoryComponent>(target)
            && _inventory.HasSlot(target, NeckSlot);
    }
}
