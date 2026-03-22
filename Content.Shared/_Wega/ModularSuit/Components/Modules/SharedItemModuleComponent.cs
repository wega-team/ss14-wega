using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[RegisterComponent, NetworkedComponent]
public abstract partial class SharedItemModuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Module;
}
