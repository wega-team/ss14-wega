using Robust.Shared.GameStates;

namespace Content.Shared.GPS.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HandheldGpsUiComponent : Component
{
    [DataField]
    public bool BroadcastEnabled = true;
}
