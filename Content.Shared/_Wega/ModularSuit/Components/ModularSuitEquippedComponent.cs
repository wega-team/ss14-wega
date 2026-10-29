using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModularSuitEquippedComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> EquippedParts = new();

    [DataField, AutoNetworkedField]
    public EntityUid Wearer;
}
