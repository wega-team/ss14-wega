using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland.Events;

[Serializable, NetSerializable]
public sealed partial class DragonBloodDoAfterEvent : SimpleDoAfterEvent { }

public sealed partial class BecomeToDrakeActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> LowerDrake = "LowerAshDrakePolymorph";

    [DataField]
    public EntProtoId ReturnBack = "DrakeReturnBackAction";
}

public sealed partial class DrakeReturnBackActionEvent : InstantActionEvent
{
}
