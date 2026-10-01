using Content.Shared.Nutrition.Prototypes;

namespace Content.Shared.Feroxi;

[RegisterComponent]
public sealed partial class BloodSenseComponent : Component
{
    [DataField]
    public float Radius = 10f;

    [DataField]
    public float WallFactor = 0.4f;

    [DataField]
    public float Threshold = 0.3f;

    [DataField]
    public float MinPressure = 10f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public float ThirstCost = 0.2f;

    [DataField]
    public SatiationValue AutoOffThreshold = "Parched";

    public EntityUid? ActionEntity;

    [DataField]
    public float FullStrength = 6f;

    [DataField]
    public Color SilhouetteColor = Color.FromHex("#ff1a1a");

    [DataField]
    public float MinAlpha = 0.25f;

    [DataField]
    public float MaxAlpha = 0.85f;

    [DataField]
    public float PulsePeriod = 1.2f;

    [DataField]
    public float PulseDepth = 0.4f;

    [DataField]
    public float RedHueRange = 12f;

    [DataField]
    public float MinSaturation = 0.5f;

    [DataField]
    public float MinValue = 0.2f;

    [DataField]
    public float Darkness = 0.25f;

    [DataField]
    public float Vignette = 0.6f;
}
