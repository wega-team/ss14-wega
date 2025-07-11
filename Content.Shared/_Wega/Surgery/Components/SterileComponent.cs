namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class SterileComponent : Component
{
    [DataField("amount")]
    public float Amount = 100f;

    [DataField("decayRate")]
    public float DecayRate = 0.5f;

    [DataField("alwaysSterile")]
    public bool AlwaysSterile = false;

    [ViewVariables]
    public float NextUpdateTick = default!;
}
