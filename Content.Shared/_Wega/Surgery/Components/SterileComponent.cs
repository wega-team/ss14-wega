namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class SterileComponent : Component
{
    [DataField]
    public float Amount = 0f;

    [DataField]
    public float DecayRate = 0.5f;

    [DataField]
    public bool AlwaysSterile = false;

    [ViewVariables]
    public float NextUpdateTick = default!;
}
