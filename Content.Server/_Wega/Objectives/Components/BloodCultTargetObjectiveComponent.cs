using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A common elimination task.
/// </summary>
[RegisterComponent, Access(typeof(BloodCultTargetObjectiveSystem))]
public sealed partial class BloodCultTargetObjectiveComponent : Component;
