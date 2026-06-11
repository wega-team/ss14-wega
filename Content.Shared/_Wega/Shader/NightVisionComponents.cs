using Robust.Shared.GameStates;
using Content.Shared.Overlays;

namespace Content.Shared.Shaders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NightVisionComponent : ToggleableHudComponent
{
    [DataField("brightness"), AutoNetworkedField]
    public float Brightness = 1f;

    [DataField("tint"), AutoNetworkedField]
    public Color Tint = Color.FromHex("#1c89f2");

    [DataField("luminanceThreshold"), AutoNetworkedField]
    public float LuminanceThreshold = 0f;

    [DataField("noiseAmount"), AutoNetworkedField]
    public float NoiseAmount = 0.075f;
}
