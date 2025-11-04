using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed class HeartSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnHeartInit);
        SubscribeLocalEvent<HeartComponent, EntityUnpausedEvent>(OnHeartUnpaused);
        SubscribeLocalEvent<HeartComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);

        SubscribeLocalEvent<HeartComponent, OrganAddedToBodyEvent>(OnHeartAddedToBody);
        SubscribeLocalEvent<HeartComponent, OrganRemovedFromBodyEvent>(OnHeartRemovedFromBody);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeartComponent>();
        while (query.MoveNext(out var heartUid, out var heart))
        {
            if (_gameTiming.CurTime < heart.NextBeatTime)
                continue;

            heart.NextBeatTime += heart.BeatInterval;

            var bodyUid = heart.Body ?? GetBodyForOrgan(heartUid);
            if (bodyUid == null || !_mobStateSystem.IsAlive(bodyUid.Value))
                continue;

            PumpBlood(bodyUid.Value, heart);
        }
    }

    private void OnHeartInit(Entity<HeartComponent> entity, ref ComponentInit args)
    {
        entity.Comp.NextBeatTime = _gameTiming.CurTime + entity.Comp.BeatInterval;
    }

    private void OnHeartUnpaused(Entity<HeartComponent> entity, ref EntityUnpausedEvent args)
    {
        entity.Comp.NextBeatTime += args.PausedTime;
    }

    private void OnHeartAddedToBody(Entity<HeartComponent> ent, ref OrganAddedToBodyEvent args)
    {
        ent.Comp.Body = args.Body;
    }

    private void OnHeartRemovedFromBody(Entity<HeartComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        ent.Comp.Body = null;
    }

    private EntityUid? GetBodyForOrgan(EntityUid organUid)
    {
        if (TryComp<OrganComponent>(organUid, out var organ))
            return organ.Body;

        return null;
    }

    private void PumpBlood(EntityUid bodyUid, HeartComponent heart)
    {
        if (!HasComp<BloodstreamComponent>(bodyUid))
            return;

        var ev = new HeartBeatEvent(heart.Efficiency);
        RaiseLocalEvent(bodyUid, ref ev);

        if (heart.Efficiency < heart.MinEfficiencyForLife)
            HandleHeartFailure(bodyUid, heart);
        else
            _alertsSystem.ClearAlert(bodyUid, heart.HeartFailureAlert);
    }

    private void HandleHeartFailure(EntityUid uid, HeartComponent heart)
    {
        _alertsSystem.ShowAlert(uid, heart.HeartFailureAlert);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Bloodloss", 0.05f);
        _damageableSystem.TryChangeDamage(uid, damage);
    }

    private void OnApplyMetabolicMultiplier(Entity<HeartComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.BeatInterval *= args.Multiplier;
    }

    /// <summary>
    /// Modifies heart efficiency (0-1).
    /// </summary>
    public void ModifyEfficiency(EntityUid uid, float amount, HeartComponent? heart = null)
    {
        if (!Resolve(uid, ref heart))
            return;

        heart.Efficiency = Math.Clamp(heart.Efficiency + amount, 0f, 1f);
    }

    /// <summary>
    /// Sets heart beat interval.
    /// </summary>
    public void SetBeatInterval(EntityUid uid, TimeSpan interval, HeartComponent? heart = null)
    {
        if (!Resolve(uid, ref heart))
            return;

        heart.BeatInterval = interval;
    }
}

[ByRefEvent]
public record struct HeartBeatEvent(float Efficiency);
