using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition: complete when the target has a non-None criminal record status.
/// Depends on <see cref="TargetObjectiveComponent"/> for the target.
/// </summary>
[RegisterComponent, Access(typeof(NinjaNewObjectivesSystem))]
public sealed partial class NinjaFrameConditionComponent : Component { }
