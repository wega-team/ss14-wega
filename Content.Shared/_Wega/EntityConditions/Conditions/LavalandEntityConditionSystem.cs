using Content.Shared.Lavaland.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class LavalandEntityConditionSystem : EntityConditionSystem<TransformComponent, LavalandCondition>
{
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<LavalandCondition> args)
    {
        var userTransform = Transform(entity);
		
        if (HasComp<LavalandComponent>(userTransform.MapUid))
            args.Result = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class LavalandCondition : EntityConditionBase<LavalandCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => Loc.GetString("entity-condition-guidebook-body-lavaland");
}