using Robust.Shared.GameStates;

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawEntryComponent : Component
{
    public const string HolderPrototypeId = "VentCrawHolder";
}
