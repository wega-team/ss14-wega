namespace Content.Shared.Flash.Components;

[RegisterComponent]
public sealed partial class FlashImmunityIgnoreComponent : Component
{
    /// <summary>
    /// Multiplier applied to flash duration when target has flash protection.
    /// </summary>
    [DataField]
    public float FlashReduction = 1f;
}
