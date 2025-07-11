using Robust.Shared.GameStates;

namespace Content.Shared.Shaders;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField("brightness")]
    public float Brightness = 1.5f;

    [DataField("tint")]
    public Color Tint = Color.FromHex("#1c89f2");

    [DataField("luminanceThreshold")]
    public float LuminanceThreshold = 0.5f;

    [DataField("noiseAmount")]
    public float NoiseAmount = 0.075f;
}
