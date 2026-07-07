using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Misc.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Misc;

public sealed partial class WeaponHotswapSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponHotswapComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<WeaponHotswapComponent, MarkerAttackAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<WeaponHotswapComponent, AfterMarkerAttackedEvent>(RelayEvent);
        SubscribeLocalEvent<WeaponHotswapComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<WeaponHotswapComponent, GunShotEvent>(RelayEvent);

        SubscribeLocalEvent<WeaponHotswapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WeaponHotswapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WeaponHotswapComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<WeaponHotswapComponent, ExaminedEvent>(OnExamine);
    }

    private void RelayEvent<T>(Entity<WeaponHotswapComponent> ent, ref T args) where T : notnull
    {
        if (ent.Comp.IsAlternate && ent.Comp.PairedWeapon != null)
        {
            RaiseLocalEvent(ent.Comp.PairedWeapon.Value, ref args);
        }
    }

    private void OnMapInit(Entity<WeaponHotswapComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.IsAlternate)
            return;

        var container = _container.EnsureContainer<Container>(ent.Owner, WeaponHotswapComponent.PairedWeaponContainerId);
        if (container.ContainedEntities.Count > 0)
            return;

        var alternate = Spawn(ent.Comp.AlternateForm, Transform(ent.Owner).Coordinates);

        var altHotswap = EnsureComp<WeaponHotswapComponent>(alternate);
        altHotswap.PairedWeapon = ent.Owner;

        _container.Insert(alternate, container);
        ent.Comp.PairedWeapon = alternate;

        Dirty(ent.Owner, ent.Comp);
        Dirty(alternate, altHotswap);
    }

    private void OnUseInHand(Entity<WeaponHotswapComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (ent.Comp.PairedWeapon != null && TryComp<WeaponHotswapComponent>(ent.Comp.PairedWeapon, out var pairedComp))
        {
            if (!_container.TryGetContainer(ent.Owner, WeaponHotswapComponent.PairedWeaponContainerId, out var container))
                return;

            if (!container.Contains(ent.Comp.PairedWeapon.Value))
                return;

            SwapWeapons(args.User, ent.Owner, ent.Comp.PairedWeapon.Value);

            _useDelay.TryResetDelay(ent.Owner);
            _useDelay.TryResetDelay(ent.Comp.PairedWeapon.Value);

            var message = ent.Comp.IsAlternate
                ? Loc.GetString("weapon-hotswap-switch-main")
                : Loc.GetString("weapon-hotswap-switch-alt");

            _popup.PopupEntity(message, ent.Owner, args.User);

            var name = Identity.Name(args.User, EntityManager);
            var otherMessage = ent.Comp.IsAlternate
                ? Loc.GetString("weapon-hotswap-switch-main-other", ("user", name))
                : Loc.GetString("weapon-hotswap-switch-alt-other", ("user", name));

            var filter = Filter.Pvs(ent.Comp.PairedWeapon.Value, entityManager: EntityManager);
            filter.RemovePlayerByAttachedEntity(args.User);

            _popup.PopupEntity(otherMessage, ent.Comp.PairedWeapon.Value, filter, false, PopupType.Small);
            _audio.PlayPvs(ent.Comp.SwapSound, ent.Comp.PairedWeapon.Value);

            args.Handled = true;
        }
    }

    private void SwapWeapons(EntityUid user, EntityUid current, EntityUid target)
    {
        if (!_container.TryGetContainer(current, WeaponHotswapComponent.PairedWeaponContainerId, out var currentContainer))
            return;

        if (!_container.TryGetContainer(target, WeaponHotswapComponent.PairedWeaponContainerId, out var targetContainer))
            return;

        if (!_container.Remove(target, currentContainer, force: true))
            return;

        if (!_container.Insert(current, targetContainer, force: true))
        {
            _container.Insert(target, currentContainer, force: true);
            return;
        }

        var hand = _hands.GetActiveHand(user);
        if (hand != null) _hands.TryForcePickup(user, target, hand);
    }

    private void OnTerminating(Entity<WeaponHotswapComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.PairedWeapon != null && Exists(ent.Comp.PairedWeapon.Value))
            QueueDel(ent.Comp.PairedWeapon.Value);
    }

    private void OnExamine(Entity<WeaponHotswapComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(WeaponHotswapComponent)))
        {
            if (ent.Comp.IsAlternate)
            {
                args.PushMarkup(Loc.GetString("weapon-hotswap-examine-alt"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("weapon-hotswap-examine-main"));
            }

            if (ent.Comp.PairedWeapon != null && Exists(ent.Comp.PairedWeapon.Value))
            {
                var pairedName = Name(ent.Comp.PairedWeapon.Value);
                args.PushMarkup(Loc.GetString("weapon-hotswap-examine-paired", ("name", pairedName)));
            }
        }
    }
}
