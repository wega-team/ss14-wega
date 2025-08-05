using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Android;

public sealed partial class ToggleLockActionEvent : InstantActionEvent;

[RegisterComponent, NetworkedComponent]
public sealed partial class AndroidComponent : Component
{
    [DataField]
    public float DischargeSpeedModifier = 0.3f;
    public TimeSpan DischargeTime;
    public TimeSpan NextDischargeStun;

    [DataField]
    public string ToggleLockAction = "ActionToggleLock";

    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}
