using Content.Shared.Damage.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Flash.Components;
using Content.Server.Bible.Components;
using Robust.Shared.Timing;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private void InitializePowers()
    {
        InitializeHemomancer();
        InitializeUmbrae();
        InitializeGargantua();
        InitializeDantalion();

        // Basic Abilities
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateActionEvent>(OnRejuvenate);
        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnVampireGlare);
    }

    #region Basic Abilities

    private void OnRejuvenate(EntityUid uid, VampireComponent component, VampireRejuvenateActionEvent args)
    {
        if (_mobState.IsDead(uid))
        {
            _popup.PopupEntity(Loc.GetString("vampire-heal-dead"), uid, uid, PopupType.MediumCaution);
            return;
        }

        TryRemoveKnockdown(uid);
        _stamina.RemoveStaminaDamage(uid);

        if (component.CurrentBlood >= args.BloodCost)
        {
            ExecuteRejuvenateHealTick(uid, 0, args);
        }

        args.Handled = true;
    }

    private void ExecuteRejuvenateHealTick(EntityUid uid, int currentTick, VampireRejuvenateActionEvent args)
    {
        if (currentTick >= args.Repeats)
            return;

        _damage.TryChangeDamage(uid, args.Heal, true, false, origin: uid);

        Timer.Spawn(args.TimeInterval, () => ExecuteRejuvenateHealTick(uid, currentTick + 1, args));
    }

    private void OnVampireGlare(EntityUid vampire, VampireComponent component, VampireGlareActionEvent args)
    {
        var target = args.Target;
        if (HasComp<VampireComponent>(target) || HasComp<FlashImmunityComponent>(target))
            return;

        if (HasComp<BibleUserComponent>(target) && !HasTruePower(vampire))
        {
            _stun.TryUpdateParalyzeDuration(vampire, TimeSpan.FromSeconds(5f));
            _chat.TryEmoteWithoutChat(vampire, _proto.Index(Scream), true);
            _damage.TryChangeDamage(vampire, component.HolyDamage);
            return;
        }

        args.Handled = true;

        var ev = new FlashAttemptEvent(target, vampire, null);
        RaiseLocalEvent(target, ref ev, true);
        if (ev.Cancelled)
            return;

        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(5f));
    }

    #endregion

    #region Utility Methods

    private void SendFailedPopup(EntityUid uid)
    {
        _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
    }

    private bool TryRemoveKnockdown(Entity<StaminaComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        _status.TryRemoveStatusEffect(entity.Owner, SharedStunSystem.StunId);
        _stun.ForceStandUp(entity.Owner);
        return true;
    }

    private bool TrySpawnObjectAtPosition(EntityCoordinates coords, EntProtoId entityId, EntityUid uid)
    {
        var grid = _transform.GetGrid(coords);
        if (grid == null) return false;

        var gridEntityUid = grid.Value;
        if (!TryComp<MapGridComponent>(gridEntityUid, out var gridComp))
            return false;

        var position = coords.Position;
        var gridPosition = new Vector2i((int)position.X, (int)position.Y);
        if (!_map.TryGetTileRef(gridEntityUid, gridComp, gridPosition, out var tileRef)
            || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            return false;

        var ent = Spawn(entityId, coords);

        var comp = EnsureComp<PreventCollideComponent>(ent);
        comp.Uid = uid;

        return true;
    }

    #endregion
}
