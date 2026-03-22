using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModularSuitCarrierComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public string? CurrentSlot = default!;
}
