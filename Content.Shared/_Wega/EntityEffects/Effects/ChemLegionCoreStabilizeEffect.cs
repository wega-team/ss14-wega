using Content.Shared.Lavaland.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ChemLegionCoreStabilizeEffectSystem : EntityEffectSystem<LegionCoreComponent, ChemLegionCoreStabilize>
{
    protected override void Effect(Entity<LegionCoreComponent> entity, ref EntityEffectEvent<ChemLegionCoreStabilize> args)
    {
        if (entity.Comp.AlwaysActive || !entity.Comp.Active)
            return;

        entity.Comp.AlwaysActive = true;
        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemLegionCoreStabilize : EntityEffectBase<ChemLegionCoreStabilize>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-legion-core-stabilize");
}
