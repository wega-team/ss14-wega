using Content.Shared.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Reinforces slime extracts, preventing them from being destroyed.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemReinforcedExtractsEntityEffectSystem : EntityEffectSystem<SlimeGrowthComponent, ChemReinforcedExtracts>
{
    protected override void Effect(Entity<SlimeGrowthComponent> entity, ref EntityEffectEvent<ChemReinforcedExtracts> args)
    {
        if (entity.Comp.Reinforced)
            return;

        entity.Comp.Reinforced = true;
        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemReinforcedExtracts : EntityEffectBase<ChemReinforcedExtracts>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reinforce-extracts");
}
