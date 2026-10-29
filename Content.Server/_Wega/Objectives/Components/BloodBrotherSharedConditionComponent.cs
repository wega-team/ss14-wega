using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A basic component for the common goals of blood brothers
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherSharedConditionSystem))]
public sealed partial class BloodBrotherSharedConditionComponent : Component
{
    /// <summary>
    /// ID brother mind
    /// </summary>
    [DataField]
    public EntityUid? BrotherMind;

    /// <summary>
    /// Do you need both brothers to be alive to achieve your goals?
    /// </summary>
    [DataField]
    public bool RequireBothAlive = true;
}
