using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Weapons.Ranged.Events;

[Serializable, NetSerializable]
public sealed class FlameBashVisualEvent(EntProtoId prototype, NetCoordinates coordinates, Angle rotation) : EntityEventArgs
{
    public EntProtoId Prototype = prototype;
    public NetCoordinates Coordinates = coordinates;
    public Angle Rotation = rotation;
}