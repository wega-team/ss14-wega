using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Android;

public sealed partial class ToggleLockActionEvent : InstantActionEvent;

[RegisterComponent, NetworkedComponent]
public sealed partial class AndroidComponent : Component
{
    [DataField]
    public float DischargeSpeedModifier = 0.4f;
    [DataField]
    public SoundSpecifier DischargeStunSound = new SoundCollectionSpecifier("CargoError");
    public TimeSpan DischargeTime;
    public TimeSpan NextDischargeStun;

    [DataField]
    public string ToggleLockAction = "ActionToggleLock";
    public EntityUid? ToggleLockActionEntity;

    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    [DataField]
    public float BasePointLightRadiuse = 2.5f;
    [DataField]
    public float BasePointLightEnergy = 1.2f;
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LightEntity;
    [DataField]
    public SoundSpecifier ToggleLightSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    [DataField("lightPrototype")]
    public String LightEntityPrototype = "AndroidLightMarker";
    [DataField]
    public string TogglelLightAction = "ActionToggleAndroidLeds";
    public EntityUid? ToggleLightActionEntity;
}
