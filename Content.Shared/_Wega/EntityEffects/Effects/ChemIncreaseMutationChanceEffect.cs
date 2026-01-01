using Content.Shared.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Increases mutation chance for slimes.
/// The mutation chance increase is equal to <see cref="ChemIncreaseMutationChance.FixedIncrease"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemIncreaseMutationChanceEntityEffectSystem : EntityEffectSystem<SlimeGrowthComponent, ChemIncreaseMutationChance>
{
    protected override void Effect(Entity<SlimeGrowthComponent> entity, ref EntityEffectEvent<ChemIncreaseMutationChance> args)
    {
        var fixedIncrease = args.Effect.FixedIncrease * args.Scale;
        var maxMutationChance = args.Effect.MaxMutationChance;

        entity.Comp.MutationChance = Math.Min(entity.Comp.MutationChance + fixedIncrease, maxMutationChance);

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemIncreaseMutationChance : EntityEffectBase<ChemIncreaseMutationChance>
{
    /// <summary>
    ///     Fixed amount to increase mutation chance by.
    /// </summary>
    [DataField]
    public float FixedIncrease = 0.12f;

    /// <summary>
    ///     Maximum mutation chance.
    /// </summary>
    [DataField]
    public float MaxMutationChance = 0.95f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-increase-mutation-chance",
            ("increase", (int)(FixedIncrease * 100)),
            ("max", (int)(MaxMutationChance * 100)));
}
