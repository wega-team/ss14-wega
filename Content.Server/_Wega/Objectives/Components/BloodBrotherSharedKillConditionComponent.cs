using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A component for the common purpose of killing blood brothers
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedKillConditionSystem))]
public sealed partial class BloodBrotherSharedKillConditionComponent : Component
{
    /// <summary>
    /// Whether the target must be dead
    /// </summary>
    [DataField]
    public bool RequireDead = true;

    /// <summary>
    /// Whether the target must not escape
    /// </summary>
    [DataField]
    public bool RequireMaroon = true;
}
