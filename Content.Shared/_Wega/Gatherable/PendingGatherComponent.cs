using Robust.Shared.GameStates;

namespace Content.Shared.Gatherable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GatherableSystem))]
public sealed partial class PendingGatherComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Hits;
}
