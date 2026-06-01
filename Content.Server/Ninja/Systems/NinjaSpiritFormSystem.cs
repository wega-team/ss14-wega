using Content.Shared.CombatMode.Pacification;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Linq;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the Spirit Form ability: phase through walls while draining 2% of max battery charge per second.
/// </summary>
public sealed class NinjaSpiritFormSystem : EntitySystem
{
    private const string SuitDisableDelayId = "suit_powers";

    [Dependency] private readonly NinjaCloakSystem _cloak = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, ToggleSpiritFormEvent>(OnToggle);
        // Use SpiritFormComponent (not NinjaSuitComponent) to avoid duplicate subscription with SharedNinjaSuitSystem
        SubscribeLocalEvent<SpiritFormComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<SpaceNinjaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NinjaSuitComponent, SpiritFormComponent>();
        while (query.MoveNext(out var uid, out var suit, out var form))
        {
            if (!form.Active)
                continue;

            var user = Transform(uid).ParentUid;
            if (!_ninja.NinjaQuery.HasComp(user))
                continue;

            if (!_ninja.GetNinjaBattery(user, out var battUid, out var battComp))
                continue;

            var drain = battComp.MaxCharge * form.DrainRate * frameTime;
            if (!_battery.TryUseCharge((battUid.Value, battComp), drain))
                Deactivate((uid, suit, form), user, outOfPower: true);
        }
    }

    private void OnRefreshSpeed(Entity<SpaceNinjaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var ninja = ent.Comp;
        if (ninja.Suit is not { } suitUid)
            return;

        if (!TryComp<SpiritFormComponent>(suitUid, out var form) || !form.Active)
            return;

        args.ModifySpeed(form.SpeedModifier, form.SpeedModifier);
    }

    private void OnToggle(Entity<NinjaSuitComponent> ent, ref ToggleSpiritFormEvent args)
    {
        if (!TryComp<SpiritFormComponent>(ent, out var form))
            return;

        var user = args.Performer;

        if (!form.Active && _cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        // respect the suit's 5-sec post-hit disable
        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
        {
            _popup.PopupEntity(Loc.GetString("ninja-suit-cooldown"), user, user);
            return;
        }

        if (form.Active)
            Deactivate((ent, ent.Comp, form), user);
        else
            Activate((ent, ent.Comp, form), user);
    }

    private void OnUnequipped(Entity<SpiritFormComponent> ent, ref GotUnequippedEvent args)
    {
        if (!ent.Comp.Active || !TryComp<NinjaSuitComponent>(ent, out var suit))
            return;

        Deactivate((ent, suit, ent.Comp), args.EquipTarget);
    }

    private void Activate(Entity<NinjaSuitComponent, SpiritFormComponent> ent, EntityUid user)
    {
        var form = ent.Comp2;

        // check spirit-form-specific cooldown
        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), form.CooldownDelayId))
        {
            _popup.PopupEntity(Loc.GetString("ninja-spirit-form-cooldown"), user, user);
            return;
        }

        if (!_ninja.GetNinjaBattery(user, out _, out var battComp) ||
            _battery.GetCharge((default, battComp)) <= 0)
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // save and replace collision on the ninja mob
        if (TryComp<FixturesComponent>(user, out var fixtures) &&
            TryComp<PhysicsComponent>(user, out var physics))
        {
            var pair = fixtures.Fixtures.FirstOrDefault();
            if (pair.Value != null)
            {
                form.OriginalCollisionMask = pair.Value.CollisionMask;
                form.OriginalCollisionLayer = pair.Value.CollisionLayer;
                _physics.SetCollisionMask(user, pair.Key, pair.Value,
                    (int) CollisionGroup.GhostImpassable, fixtures, physics);
                _physics.SetCollisionLayer(user, pair.Key, pair.Value,
                    (int) CollisionGroup.GhostImpassable, fixtures, physics);
            }
        }

        // enforce pacifism for the duration of spirit form
        if (!HasComp<PacifiedComponent>(user))
        {
            EnsureComp<PacifiedComponent>(user);
            form.AddedPacifism = true;
        }

        form.Active = true;
        Dirty(ent.Owner, form);
        _movementSpeed.RefreshMovementSpeedModifiers(user);
        _popup.PopupEntity(Loc.GetString("ninja-spirit-form-activate"), user, user);
    }

    public void Deactivate(Entity<NinjaSuitComponent, SpiritFormComponent> ent, EntityUid user, bool outOfPower = false)
    {
        var form = ent.Comp2;

        if (form.OriginalCollisionMask is { } mask &&
            TryComp<FixturesComponent>(user, out var fixtures) &&
            TryComp<PhysicsComponent>(user, out var physics))
        {
            var pair = fixtures.Fixtures.FirstOrDefault();
            if (pair.Value != null)
            {
                _physics.SetCollisionMask(user, pair.Key, pair.Value, mask, fixtures, physics);
                _physics.SetCollisionLayer(user, pair.Key, pair.Value,
                    form.OriginalCollisionLayer!.Value, fixtures, physics);
            }
            form.OriginalCollisionMask = null;
            form.OriginalCollisionLayer = null;
        }

        if (form.AddedPacifism)
        {
            RemComp<PacifiedComponent>(user);
            form.AddedPacifism = false;
        }

        form.Active = false;
        Dirty(ent.Owner, form);
        _movementSpeed.RefreshMovementSpeedModifiers(user);

        // start 25-sec cooldown
        var useDelay = EnsureComp<UseDelayComponent>(ent.Owner);
        _useDelay.SetLength((ent.Owner, useDelay), form.CooldownTime, id: form.CooldownDelayId);
        _useDelay.TryResetDelay((ent.Owner, useDelay), id: form.CooldownDelayId);

        var message = outOfPower ? "ninja-spirit-form-no-power" : "ninja-spirit-form-deactivate";
        _popup.PopupEntity(Loc.GetString(message), user, user);
    }
}
