using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RaveOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color BaseColor = Color.FromHex("#ff3ce6");

    [DataField, AutoNetworkedField]
    public Color SecondaryColor = Color.FromHex("#3c9eff");

    [DataField, AutoNetworkedField]
    public float PulseSpeed = 0.3f;

    [DataField, AutoNetworkedField]
    public float Intensity = 0.8f;

    [DataField, AutoNetworkedField]
    public float GrainStrength = 0.25f;

    [DataField, AutoNetworkedField]
    public float Distortion = 0.15f;
}
