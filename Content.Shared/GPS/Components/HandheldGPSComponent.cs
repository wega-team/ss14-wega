using Robust.Shared.GameStates;

namespace Content.Shared.GPS.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HandheldGPSComponent : Component
{
    [DataField]
    public float UpdateRate = 1.5f;

    // Corvax-Wega-Lavaland-start
    [DataField]
    public string GPSName = string.Empty;

    [DataField]
    public string GPSDesc = "handheld-gps-desc-base";
    // Corvax-Wega-Lavaland-end
}
