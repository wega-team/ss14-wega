namespace Content.Server.Ninja.Components;

/// <summary>
/// Added to the borg brain (MMI/posibrain) entity when a ninja successfully steals it.
/// While present on a brain that is inside any borg chassis, that chassis is treated as
/// a ninja borg. Removing the brain from the chassis reverts the chassis to standard.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaBorgBrainChipComponent : Component
{
    /// <summary>The ninja entity that performed the steal.</summary>
    [DataField]
    public EntityUid NinjaOwner;
}
