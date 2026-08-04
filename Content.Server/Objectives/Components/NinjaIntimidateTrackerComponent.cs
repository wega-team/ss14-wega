using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Added to a target entity by <see cref="NinjaIntimidateConditionComponent"/> on objective assignment.
/// Accumulates incoming damage into the condition component.
/// </summary>
[RegisterComponent, Access(typeof(NinjaNewObjectivesSystem))]
public sealed partial class NinjaIntimidateTrackerComponent : Component
{
    [DataField]
    public EntityUid ConditionEntity;
}
