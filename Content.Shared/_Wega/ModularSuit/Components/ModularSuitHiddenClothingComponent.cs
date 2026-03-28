using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModularSuitHiddenClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> HiddenItems = new();

    [DataField, AutoNetworkedField]
    public float ArmorEfficiency = 0.1f;
}
