using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A goal that requires completion of the ritual.
/// </summary>
[RegisterComponent, Access(typeof(BloodCultRitualObjectiveSystem))]
public sealed partial class BloodCultRitualObjectiveComponent : Component;
