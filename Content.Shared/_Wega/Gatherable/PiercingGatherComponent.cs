using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Gatherable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PiercingGatherComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Depth = 2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxDurability = FixedPoint2.New(150);

    [ViewVariables]
    public readonly HashSet<EntityUid> Pierced = new();
}
