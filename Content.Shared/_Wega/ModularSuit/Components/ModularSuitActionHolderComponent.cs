using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModularSuitActionHolderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntProtoId, EntityUid> ModuleActions = new();
}
