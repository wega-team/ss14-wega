using Content.Server._Wega.Chemistry.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ChiurizinOverdoseEffectSystem : EntityEffectSystem<MetaDataComponent, ChiurizinOverdose>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly Content.Shared.StatusEffect.StatusEffectsSystem _statusOld = default!;
    [Dependency] private readonly StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly DamageSpecifier BrainDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Cellular"] = FixedPoint2.New(5),
        }
    };

    private static readonly DamageSpecifier SmallBluntDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Blunt"] = FixedPoint2.New(0.5f),
        }
    };

    private static readonly DamageSpecifier HypoxiaDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Asphyxiation"] = FixedPoint2.New(500),
        }
    };

    private static readonly string[] BackPainMessages =
    {
        "chiurizin-overdose-back-pain-1",
        "chiurizin-overdose-back-pain-2",
        "chiurizin-overdose-back-pain-3",
    };

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChiurizinOverdose> args)
    {
        var uid = entity.Owner;

        if (!HasComp<MobStateComponent>(uid) || _mobState.IsDead(uid))
            return;

        TryComp<HumanoidProfileComponent>(uid, out var profile);
        var age = profile?.Age ?? 0;

        // (100 - age)% chance, minimum 10%: age +1-3 years
        var agingChance = Math.Max(0.1f, (100f - age) / 100f);
        if (_random.Prob(agingChance) && profile != null)
            _humanoid.SetAge(uid, profile.Age + _random.Next(1, 4), profile);

        // 50%: 0.5 blunt damage (back trauma) + optional private popup
        if (_random.Prob(0.5f))
        {
            _damageable.TryChangeDamage(uid, SmallBluntDamage, ignoreResistances: true);
            if (_random.Prob(0.25f))
            {
                var msg = Loc.GetString(_random.Pick(BackPainMessages));
                _popup.PopupEntity(msg, uid, uid, PopupType.SmallCaution);
            }
        }

        // 20%: knockdown for 3 seconds
        if (_random.Prob(0.2f))
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(3), refresh: false);

        // 10%: teleport to saved position
        if (_random.Prob(0.1f) && TryComp<ChiurizinStateComponent>(uid, out var state) && state.SavedPosition.HasValue)
            _xform.SetMapCoordinates(uid, state.SavedPosition.Value);

        // 2%: forced sleep for 10 seconds
        if (_random.Prob(0.02f))
            _statusNew.TryAddStatusEffectDuration(uid, Content.Shared.Bed.Sleep.SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(10));

        // 1%: temporary blindness (5s) + 5 cellular damage
        if (_random.Prob(0.01f))
        {
            _statusOld.TryAddStatusEffect<TemporaryBlindnessComponent>(
                uid,
                TemporaryBlindnessSystem.BlindingStatusEffect,
                TimeSpan.FromSeconds(5),
                refresh: false);
            _damageable.TryChangeDamage(uid, BrainDamage, ignoreResistances: true);
        }

        // Age 100+: dice roll until 1 or 6
        if (age >= 100)
            ApplyExtremeAgeEffect(uid, profile);
    }

    private void ApplyExtremeAgeEffect(EntityUid uid, HumanoidProfileComponent? profile)
    {
        while (true)
        {
            var roll = _random.Next(1, 7);
            if (roll == 1)
            {
                _damageable.TryChangeDamage(uid, HypoxiaDamage, ignoreResistances: true);
                return;
            }
            if (roll == 6)
            {
                if (profile != null)
                    _humanoid.SetAge(uid, 90, profile);
                if (TryComp<ChiurizinStateComponent>(uid, out var state))
                    state.CycleCount = 0;
                _rejuvenate.PerformRejuvenate(uid);
                return;
            }
        }
    }
}
