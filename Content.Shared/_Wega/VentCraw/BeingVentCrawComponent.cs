using Robust.Shared.GameStates;

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BeingVentCrawComponent : Component
{
    [ViewVariables]
    public EntityUid Holder;
}
