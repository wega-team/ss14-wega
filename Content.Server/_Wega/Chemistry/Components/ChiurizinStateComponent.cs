using Robust.Shared.Map;

namespace Content.Server._Wega.Chemistry.Components;

/// <summary>
/// Tracks per-entity Chiurizin state: how many metabolism cycles have elapsed,
/// how many years have been rejuvenated, and the last saved teleport position.
/// </summary>
[RegisterComponent]
public sealed partial class ChiurizinStateComponent : Component
{
    public int CycleCount;
    public int TotalRejuvenations;
    public MapCoordinates? SavedPosition;
}
