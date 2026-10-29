using Content.Shared.FixedPoint;

namespace Content.Shared.Gatherable.Components;

[RegisterComponent]
public sealed partial class PiercingGatherComponent : Component
{
    [DataField]
    public int Depth = 2;

    [DataField]
    public FixedPoint2 MaxDurability = FixedPoint2.New(150);

    [ViewVariables]
    public readonly HashSet<EntityUid> Pierced = new();
}
