using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Server-side logic for the Energy Net ability:
/// • Casts the net on a mob (WorldTargetAction clicking near a mob).
/// • Redirects non-caster melee damage from the netted target to the net entity.
/// • Cleans up EnergyNettedComponent when the net is destroyed.
/// </summary>
public sealed partial class NinjaEnergyNetSystem : EntitySystem
{
    private const float EnergyNetCharge = 200f;
    private const string SuitDisableDelayId = "suit_powers";
    private const float TargetSearchRadius = 0.5f;

    private static readonly SoundSpecifier NetThrowSound =
        new SoundPathSpecifier("/Audio/Effects/sparks4.ogg");

    [Dependency] private NinjaCloakSystem     _cloak     = default!;
    [Dependency] private SpaceNinjaSystem    _ninja    = default!;
    [Dependency] private SharedPopupSystem   _popup    = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem      _useDelay = default!;
    [Dependency] private SharedAudioSystem   _audio    = default!;
    [Dependency] private EntityLookupSystem  _lookup   = default!;
    [Dependency] private DamageableSystem    _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent,    NinjaEnergyNetEvent>(OnEnergyNet);
        SubscribeLocalEvent<EnergyNetComponent,    ComponentShutdown>(OnNetShutdown);
        SubscribeLocalEvent<EnergyNettedComponent, BeforeDamageChangedEvent>(OnNettedDamage);
    }

    // ── Casting ───────────────────────────────────────────────────────────────

    private void OnEnergyNet(Entity<NinjaSuitComponent> ent, ref NinjaEnergyNetEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        // Find the nearest mob at the clicked position
        var targetCoords = _transform.ToMapCoordinates(args.Target);
        EntityUid? target = null;
        foreach (var (mob, _) in _lookup.GetEntitiesInRange<MobStateComponent>(targetCoords, TargetSearchRadius))
        {
            if (mob == user)
                continue;
            target = mob;
            break;
        }

        // Clicked empty space / no valid mob: do nothing and don't start the cooldown.
        if (target == null)
            return;

        // Don't stack nets: refuse if the target is already caught in one.
        if (HasComp<EnergyNettedComponent>(target.Value))
        {
            _popup.PopupEntity(Loc.GetString("energy-net-already-netted"), user, user);
            return;
        }

        if (!_ninja.TryUseCharge(user, EnergyNetCharge))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        _audio.PlayPvs(NetThrowSound, user);

        var net = Spawn("EnergyNet", _transform.GetMoverCoordinates(target.Value));

        var netComp = EnsureComp<EnergyNetComponent>(net);
        netComp.Target = target.Value;
        Dirty(net, netComp);

        var nettedComp = EnsureComp<EnergyNettedComponent>(target.Value);
        nettedComp.Caster    = user;
        nettedComp.NetEntity = net;
        Dirty(target.Value, nettedComp);

        // Green beam between the ninja and the victim for 2 seconds (drawn client-side).
        var beam = EnsureComp<EnergyNetBeamComponent>(target.Value);
        beam.Caster = user;
        Dirty(target.Value, beam);

        var beamTarget = target.Value;
        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            if (Exists(beamTarget))
                RemComp<EnergyNetBeamComponent>(beamTarget);
        });

        _popup.PopupEntity(Loc.GetString("energy-net-trapped"), target.Value, PopupType.LargeCaution);

        // Only now (a net was actually cast) does the ability go on cooldown.
        args.Handled = true;
    }

    // ── Damage redirect ───────────────────────────────────────────────────────

    /// <summary>
    /// When a non-caster hits the netted target, redirect the damage to the net entity.
    /// SharedEnergyNetSystem has already cancelled the damage on the target side; here we
    /// apply the same damage to the net so it can be destroyed.
    /// </summary>
    private void OnNettedDamage(EntityUid uid, EnergyNettedComponent comp, ref BeforeDamageChangedEvent args)
    {
        if (comp.Caster != null && args.Origin == comp.Caster)
            return; // ninja caster hits through — do nothing

        // Always cancel damage to the netted target from non-casters
        args.Cancelled = true;

        if (comp.NetEntity is not { } net || !Exists(net))
            return;

        // Mirror the attempted damage onto the net so it can be broken
        _damageable.TryChangeDamage(net, args.Damage, ignoreResistances: true);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void OnNetShutdown(EntityUid uid, EnergyNetComponent comp, ComponentShutdown args)
    {
        if (comp.Target is not { } target || !Exists(target))
            return;

        RemComp<EnergyNettedComponent>(target);
    }
}
