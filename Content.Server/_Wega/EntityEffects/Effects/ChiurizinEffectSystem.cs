using System.Numerics;
using Content.Server._Wega.Chemistry.Components;
using Content.Server.Disease;
using Content.Server.Surgery;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Surgery.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ChiurizinEffectSystem : EntityEffectSystem<MetaDataComponent, Chiurizin>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedDrunkSystem _drunk = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AdminFrozenSystem _adminFrozen = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SurgerySystem _surgery = default!;

    private static readonly DamageSpecifier HealingDamage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            ["Blunt"] = FixedPoint2.New(-5),
            ["Slash"] = FixedPoint2.New(-5),
            ["Heat"] = FixedPoint2.New(-5),
            ["Piercing"] = FixedPoint2.New(-5),
            ["Poison"] = FixedPoint2.New(-5),
            ["Radiation"] = FixedPoint2.New(-5),
            ["Asphyxiation"] = FixedPoint2.New(-5),
            ["Bloodloss"] = FixedPoint2.New(-5),
            ["Cellular"] = FixedPoint2.New(-5),
        }
    };

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Chiurizin> args)
    {
        var uid = entity.Owner;

        if (!HasComp<MobStateComponent>(uid) || _mobState.IsDead(uid))
            return;

        var state = EnsureComp<ChiurizinStateComponent>(uid);

        // Require ≥25 units in bloodstream to activate; once started, keep going regardless of quantity.
        if (state.CycleCount == 0)
        {
            if (!_solution.TryGetSolution(uid, "bloodstream", out _, out var checkBs))
                return;

            var chiurizinQty = FixedPoint2.Zero;
            foreach (var (reagent, qty) in checkBs.Contents)
            {
                if (reagent.Prototype == "Chiurizin")
                {
                    chiurizinQty = qty;
                    break;
                }
            }
            if (chiurizinQty < FixedPoint2.New(20))
                return;
        }

        state.CycleCount++;

        // 5% stasis trap: full freeze + mute + passive healing for 5-20s.
        if (_random.Prob(0.05f) && !HasComp<ChiurizinStasisComponent>(uid))
        {
            var duration = _random.NextFloat(5f, 20f);
            _adminFrozen.FreezeAndMute(uid);
            var stasis = EnsureComp<ChiurizinStasisComponent>(uid);
            stasis.Timer = duration;
            stasis.VisualEntity = Spawn("ChiurizinStasisVisual", new EntityCoordinates(uid, Vector2.Zero));

            if (TryComp<StealthComponent>(uid, out var existingStealth))
            {
                stasis.OriginalVisibility = _stealth.GetVisibility(uid, existingStealth);
                stasis.AddedStealth = false;
            }
            else
            {
                EnsureComp<StealthComponent>(uid);
                stasis.AddedStealth = true;
            }
            _stealth.SetVisibility(uid, -1f);
        }

        // 25% save current map position.
        if (_random.Prob(0.25f))
            state.SavedPosition = _xform.GetMapCoordinates(uid);

        // Purge up to 5 other reagents from the bloodstream (1 unit each).
        if (_solution.TryGetSolution(uid, "bloodstream", out var purgeBsEntity, out var purgeBs))
        {
            var toRemove = new List<string>();
            foreach (var (reagent, _) in purgeBs.Contents)
            {
                if (reagent.Prototype == "Chiurizin")
                    continue;
                toRemove.Add(reagent.Prototype);
                if (toRemove.Count >= 5)
                    break;
            }
            foreach (var id in toRemove)
                _solution.RemoveReagent(purgeBsEntity.Value, id, FixedPoint2.New(1));
        }

        if (state.CycleCount <= 20)
            ApplyCycles1To20(uid);
        else if (state.CycleCount <= 40)
            ApplyCycles20To40(uid, state);
        else
            ApplyCycles40Plus(uid, state);
    }

    private void ApplyCycles1To20(EntityUid uid)
    {
        _drunk.TryRemoveDrunkennessTime(uid, TimeSpan.FromSeconds(4));
        _damageable.TryChangeDamage(uid, HealingDamage, ignoreResistances: true);
        _statusNew.TryRemoveTime(uid, BlindnessSystem.BlindingStatusEffect, TimeSpan.FromSeconds(2));
        _statusNew.TryRemoveTime(uid, SharedStunSystem.StunId, TimeSpan.FromSeconds(5));
        _bloodstream.TryModifyBleedAmount((uid, null), -2f);
        _surgery.ClearInternalBleeding(uid);
    }

    private void ApplyCycles20To40(EntityUid uid, ChiurizinStateComponent state)
    {
        if (_random.Prob(0.15f))
            _disease.CureAllDiseases(uid);

        if (_random.Prob(0.15f))
        {
            var ev = new CureDnaDiseaseAttemptEvent(0.15f);
            RaiseLocalEvent(uid, ev, false);
        }

        if (state.CycleCount == 30)
        {
            // Heal all damage directly — avoids RejuvenateEvent which spawns missing organs
            // and would invalidate the MetabolizerComponent iterator mid-tick.
            _damageable.ClearAllDamage(uid);
        }

        if (_random.Prob(0.25f) && TryComp<OperatedComponent>(uid, out var operated))
            _surgery.RegenerateMissingLimbs((uid, operated));
    }

    private void ApplyCycles40Plus(EntityUid uid, ChiurizinStateComponent state)
    {
        if (_random.Prob(0.5f) && state.TotalRejuvenations < 20)
        {
            if (TryComp<HumanoidProfileComponent>(uid, out var profile) && profile.Age > 18)
            {
                _humanoid.SetAge(uid, profile.Age - 1, profile);
                state.TotalRejuvenations++;
            }
        }

        if (_random.Prob(0.01f))
            state.CycleCount = 0;
    }
}
