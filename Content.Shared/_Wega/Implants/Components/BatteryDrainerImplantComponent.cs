using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent]
public sealed partial class BatteryDrainerImplantComponent : Component
{
    [DataField]
    public string ChargeAction = "ActionBodyPartImplantCharge";
    [ViewVariables]
    public EntityUid? ChargeActionEntity;

    [DataField]
    public string DischargeAction = "ActionBodyPartImplantDischarge";
    [ViewVariables]
    public EntityUid? DischargeActionEntity;

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/kinetic_reload.ogg");
}

public sealed partial class BatteryChargeActionEvent : InstantActionEvent
{
}

public sealed partial class BatteryDischargeActionEvent : InstantActionEvent
{
}
