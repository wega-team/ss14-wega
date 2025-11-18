using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawTubeComponent : Component
{
    [DataField("containerId")]
    public string ContainerId { get; set; } = "VentCrawTube";

    [ViewVariables]
    public bool Connected;

    [ViewVariables]
    public Container Contents { get; set; } = default!;
}

[ByRefEvent]
public record struct GetVentCrawsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
