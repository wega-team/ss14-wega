namespace Content.Shared.Vampire.Components;

/// <summary>
/// A component for testing vampire arson near holy sites.
/// </summary>
[RegisterComponent]
public sealed partial class HolyPointComponent : Component
{
    [DataField]
    public float Range = 6f;

    public float NextTimeTick { get; set; }
}
