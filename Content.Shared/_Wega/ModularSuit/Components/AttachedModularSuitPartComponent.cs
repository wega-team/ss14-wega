using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AttachedModularSuitPartComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Suit;
}
