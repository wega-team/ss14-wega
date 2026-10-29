using Robust.Shared.GameStates;

namespace Content.Shared.Feroxi;

[RegisterComponent, NetworkedComponent]
public sealed partial class NarcosisStatusEffectComponent : Component
{
    [DataField]
    public float FullTime = 180f;

    [DataField]
    public float RampTime = 4f;

    [DataField]
    public float Threshold = 0.6f;
}
