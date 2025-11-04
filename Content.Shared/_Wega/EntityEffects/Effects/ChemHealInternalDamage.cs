using Content.Shared.Surgery.Components;
using Content.Shared.Surgery;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Heals internal damage with a chance for each damage type.
/// The heal chance is equal to <see cref="ChemHealInternalDamage.HealChance"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemHealInternalDamageEntityEffectSystem : EntityEffectSystem<OperatedComponent, ChemHealInternalDamage>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<OperatedComponent> entity, ref EntityEffectEvent<ChemHealInternalDamage> args)
    {
        var scaledChance = args.Effect.HealChance * args.Scale;
        var damageTypes = args.Effect.DamageTypes;

        foreach (var (damageId, bodyParts) in entity.Comp.InternalDamages)
        {
            if (damageTypes != null && !damageTypes.Contains(damageId))
                continue;

            if (!_random.Prob(scaledChance))
                continue;

            if (bodyParts.Count > 0)
            {
                var healedPart = _random.Pick(bodyParts);
                bodyParts.Remove(healedPart);
            }

            if (bodyParts.Count == 0)
            {
                entity.Comp.InternalDamages.Remove(damageId);
            }
        }

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemHealInternalDamage : EntityEffectBase<ChemHealInternalDamage>
{
    /// <summary>
    ///     Chance to heal internal damage per damage type.
    /// </summary>
    [DataField("healChance")]
    public float HealChance = 0.1f;

    /// <summary>
    ///     Specific damage types to heal. If null, heals all damage types.
    /// </summary>
    [DataField("damageTypes")]
    public List<ProtoId<InternalDamagePrototype>>? DamageTypes = null;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-heal-internal-damage",
            ("chance", HealChance));
}
