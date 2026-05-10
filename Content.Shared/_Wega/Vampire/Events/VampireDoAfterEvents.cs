using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public sealed partial class VampireDrinkingBloodDoAfterEvent : SimpleDoAfterEvent
{
    public float Volume = 5;
}

[Serializable, NetSerializable]
public sealed partial class SoulAnchorDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId EntityId = "BeaconSoul";
    public FixedPoint2 BloodCost { get; }

    public SoulAnchorDoAfterEvent(FixedPoint2 cost)
    {
        BloodCost = cost;
    }
}

[Serializable, NetSerializable]
public sealed partial class EnthrallDoAfterEvent : SimpleDoAfterEvent
{
    public FixedPoint2 BloodCost { get; }

    public EnthrallDoAfterEvent(FixedPoint2 cost)
    {
        BloodCost = cost;
    }
}
