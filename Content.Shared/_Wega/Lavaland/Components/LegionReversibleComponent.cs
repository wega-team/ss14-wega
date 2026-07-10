namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class LegionReversibleComponent : Component
{
    /// <summary>
    /// Determines whether a given entity can turn into a legion.
    /// </summary>
    [DataField]
    public bool CanReversible = true;
}
