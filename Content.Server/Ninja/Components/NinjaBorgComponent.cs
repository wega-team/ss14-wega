namespace Content.Server.Ninja.Components;

/// <summary>
/// Placed on a BorgChassisNinja entity while a <see cref="NinjaBorgBrainChipComponent"/> brain is installed.
/// Stores reversion data so the original chassis can be re-spawned when the chip is removed.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaBorgComponent : Component
{
    [DataField]
    public EntityUid NinjaOwner;

    /// <summary>Entity prototype ID of the original borg chassis (e.g. "BorgChassisMedical").</summary>
    [DataField]
    public string OriginalPrototype = string.Empty;

    /// <summary>Original entity name so it can be restored on revert.</summary>
    [DataField]
    public string OriginalName = string.Empty;
}
