using Content.Shared.Xenobiology.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Stabilizes mutation chance by reducing it within a random range and setting a minimum.
/// The reduction range is applied once when the effect is triggered.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemStabilizeMutationChanceEntityEffectSystem : EntityEffectSystem<SlimeGrowthComponent, ChemStabilizeMutationChance>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<SlimeGrowthComponent> entity, ref EntityEffectEvent<ChemStabilizeMutationChance> args)
    {
        if (entity.Comp.Stabilized)
            return;

        var reductionPercent = _random.NextFloat(args.Effect.MinReduction, args.Effect.MaxReduction);
        var newChance = entity.Comp.MutationChance * (1 - reductionPercent);
        entity.Comp.MutationChance = Math.Max(newChance, args.Effect.MinMutationChance);
        entity.Comp.Stabilized = true;

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemStabilizeMutationChance : EntityEffectBase<ChemStabilizeMutationChance>
{
    /// <summary>
    ///     Minimum reduction percentage for mutation chance.
    /// </summary>
    [DataField]
    public float MinReduction = 0.15f;

    /// <summary>
    ///     Maximum reduction percentage for mutation chance.
    /// </summary>
    [DataField]
    public float MaxReduction = 0.45f;

    /// <summary>
    ///     Minimum mutation chance after stabilization.
    /// </summary>
    [DataField]
    public float MinMutationChance = 0.05f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-stabilize-mutation",
            ("minReduction", (int)(MinReduction * 100)),
            ("maxReduction", (int)(MaxReduction * 100)),
            ("min", (int)(MinMutationChance * 100)));
}
