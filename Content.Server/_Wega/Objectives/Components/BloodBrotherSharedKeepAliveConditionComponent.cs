using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A component for the common survival goals of blood brothers
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedKeepAliveConditionSystem))]
public sealed partial class BloodBrotherSharedKeepAliveConditionComponent : Component;
