using Content.Server.Beam;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lavaland.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class StormStaffSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BeamSystem _beam = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private LavalandSystem _lavaland = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StormStaffComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StormStaffComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StormStaffComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StormStaffComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Charges < comp.MaxCharges && _timing.CurTime >= comp.NextChargeTime)
            {
                comp.Charges++;
                comp.NextChargeTime = _timing.CurTime + comp.ChargeCooldown;
                _audio.PlayPvs(comp.ChargeSound, uid);
            }
        }
    }

    private void OnMapInit(Entity<StormStaffComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Charges = ent.Comp.MaxCharges;
        ent.Comp.NextChargeTime = _timing.CurTime + ent.Comp.ChargeCooldown;
    }

    private void OnAfterInteract(Entity<StormStaffComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == null || !HasComp<MobStateComponent>(args.Target))
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (ent.Comp.Charges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("storm-staff-no-charges"), ent.Owner, args.User);
            return;
        }

        if (ent.Comp.IsFiring)
            return;

        ent.Comp.IsFiring = true;
        _useDelay.TryResetDelay(ent.Owner);

        var user = args.User;
        var target = args.Target.Value;
        Timer.Spawn(ent.Comp.FireDelay, () =>
        {
            if (!Exists(ent) || !Exists(user) || !Exists(target))
            {
                ent.Comp.IsFiring = false;
                return;
            }

            if (_mobState.IsDead(user) || _mobState.IsDead(target))
            {
                ent.Comp.IsFiring = false;
                return;
            }

            if (ent.Comp.Charges <= 0)
            {
                ent.Comp.IsFiring = false;
                _popup.PopupEntity(Loc.GetString("storm-staff-no-charges"), ent.Owner, user);
                return;
            }

            ent.Comp.Charges--;

            var isEmpowered = IsWeatherActive(user);
            FireBeam(ent, user, target, isEmpowered);

            ent.Comp.IsFiring = false;
        });
    }

    private void OnUseInHand(Entity<StormStaffComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_timing.CurTime < ent.Comp.NextWeatherCancelTime)
        {
            var remaining = ent.Comp.NextWeatherCancelTime - _timing.CurTime;
            _popup.PopupEntity(Loc.GetString("storm-staff-weather-cancel-cooldown", ("time", (int)remaining.TotalMinutes + 1)), ent.Owner, args.User);
            return;
        }

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        if (!TryComp<LavalandComponent>(mapUid, out var lavaComp) || lavaComp.CurrentWeatherEntry == null)
        {
            _popup.PopupEntity(Loc.GetString("storm-staff-no-weather"), ent.Owner, args.User);
            return;
        }

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (ent.Comp.Charges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("storm-staff-no-charges"), ent.Owner, args.User);
            return;
        }

        _useDelay.TryResetDelay(ent.Owner);
        ent.Comp.Charges--;

        _lavaland.CancelWeather(mapUid.Value, lavaComp);
        ent.Comp.NextWeatherCancelTime = _timing.CurTime + ent.Comp.WeatherCancelCooldown;

        _audio.PlayPvs(ent.Comp.UseSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("storm-staff-weather-canceled"), ent.Owner, args.User, PopupType.MediumCaution);
        args.Handled = true;
    }

    private void FireBeam(Entity<StormStaffComponent> ent, EntityUid user, EntityUid target, bool isEmpowered)
    {
        var beamProto = isEmpowered ? ent.Comp.EmpoweredBeamPrototype : ent.Comp.BeamPrototype;
        _beam.TryCreateBeam(user, target, beamProto);

        if (isEmpowered)
        {
            _explosion.QueueExplosion(target, ent.Comp.ExplosionEffect, 1000f, 3f, 1.5f);
        }

        _audio.PlayPvs(ent.Comp.UseSound, target);
    }

    private bool IsWeatherActive(EntityUid user)
    {
        var mapUid = _transform.GetMap(user);
        if (mapUid == null)
            return false;

        if (!TryComp<LavalandComponent>(mapUid, out var lavaComp))
            return false;

        return lavaComp.CurrentWeatherEntry != null;
    }
}
