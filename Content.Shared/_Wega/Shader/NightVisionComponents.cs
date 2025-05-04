using Robust.Shared.GameStates;

namespace Content.Shared.Shaders;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField("brightness")]
    public float Brightness = 2.5f;

    [DataField("tint")]
    public Color Tint = Color.FromHex("#00FF00");

    [DataField("luminanceThreshold")]
    public float LuminanceThreshold = 0.5f;

    [DataField("noiseAmount")]
    public float NoiseAmount = 0.1f;
}
