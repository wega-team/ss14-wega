using Content.Shared.Physics;
using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Teleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField]
    public float TeleportationRangeStart = 4;

    [DataField]
    public int TeleportationRangeLength = 4;

    [DataField]
    public CollisionGroup CollisionGroup = CollisionGroup.MobMask;
}
