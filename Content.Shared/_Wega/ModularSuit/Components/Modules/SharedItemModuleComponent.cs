using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[Virtual]
[RegisterComponent, NetworkedComponent]
public partial class SharedItemModuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Module;
}
