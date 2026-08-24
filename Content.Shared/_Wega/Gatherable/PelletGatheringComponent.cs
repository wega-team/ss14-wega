using Robust.Shared.GameStates;

namespace Content.Shared.Gatherable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PelletGatheringComponent : Component
{
    [DataField, AutoNetworkedField]
    public int HitsRequired = 3;
}
