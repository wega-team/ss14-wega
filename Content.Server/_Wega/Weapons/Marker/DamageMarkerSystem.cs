using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Weapons.Marker;

public sealed class DamageMarkerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private const float NORMALPRESSURE = 101.325f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);
        SubscribeLocalEvent<DamageMarkerComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMarkerAttacked(EntityUid uid, DamageMarkerComponent component, AttackedEvent args)
    {
        if (component.Marker != args.Used)
            return;

        var mixture = _atmos.GetContainingMixture(uid, true, true);

        float pressureModifier = 1f;
        if (mixture != null)
        {
            var currentPressure = mixture.Pressure;

            if (currentPressure <= NORMALPRESSURE)
            {
                var normalized = currentPressure / NORMALPRESSURE;
                pressureModifier = 1 + (component.DamageMultiplier - 1) * (1 - normalized);
            }
            else
            {
                var pressureRatio = currentPressure / NORMALPRESSURE;
                pressureModifier = Math.Max(0.5f, 2 - pressureRatio);
            }
        }

        var attemptEv = new MarkerAttackAttemptEvent(args.User, uid);
        RaiseLocalEvent(args.Used, ref attemptEv);

        var baseBonus = component.Damage;
        var pressureBonus = baseBonus * (pressureModifier - 1);
        var bonus = (baseBonus + pressureBonus) * attemptEv.DamageModifier;
        args.BonusDamage += bonus;

        RemCompDeferred<DamageMarkerComponent>(uid);
        _audio.PlayPvs(component.Sound, Transform(uid).Coordinates);

        var ev = new AfterMarkerAttackedEvent(args.User, uid, bonus);
        RaiseLocalEvent(args.Used, ref ev);

        if (!_mobState.IsDead(uid) && TryComp<LeechOnMarkerComponent>(args.Used, out var leech))
        {
            var leechAmount = leech.Leech * pressureModifier * attemptEv.HealModifier;
            _damageable.TryChangeDamage(args.User, leechAmount, true, false, origin: args.Used);
        }
    }

    private void OnMeleeHit(EntityUid uid, DamageMarkerComponent component, MeleeHitEvent args)
    {
        if (!component.Weakening)
            return;

        args.BonusDamage = -(args.BaseDamage - args.BaseDamage * component.WeakeningModifier);
    }
}
