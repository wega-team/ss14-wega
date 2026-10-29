using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A component for the common escape goals of blood brothers
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedEscapeConditionSystem))]
public sealed partial class BloodBrotherSharedEscapeConditionComponent : Component;
