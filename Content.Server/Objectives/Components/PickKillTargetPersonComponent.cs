using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person
/// who is already the target of an existing <see cref="KillPersonConditionComponent"/> objective.
/// </summary>
[RegisterComponent, Access(typeof(NinjaNewObjectivesSystem))]
public sealed partial class PickKillTargetPersonComponent : Component { }
