namespace Content.Shared._Wega.Implants.Components;

/// <summary>
/// маркер для ентити под контролем разума.
/// </summary>
[RegisterComponent]
public sealed partial class MindControlComponent : Component
{
    [DataField]
    public EntityUid Master = EntityUid.Invalid;

    [DataField]
    public bool HadSyndicateFaction;

    [DataField]
    public TimeSpan? SideEffectsAt;
}
