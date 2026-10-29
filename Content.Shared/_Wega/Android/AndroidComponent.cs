using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    public float ChargeSpeed = 20f;
    [DataField]
    public float ChargeEfficency = 0.1f;
    [DataField]
    public float ChargeLimit = 0.5f;
    [DataField]
    public SoundSpecifier ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");

    [DataField]
    public float BasePointLightRadiuse = 2.5f;
    [DataField]
    public float BasePointLightEnergy = 1.6f;
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LightEntity;
    [DataField]
    public SoundSpecifier ToggleLightSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    [DataField("lightPrototype")]
    public string LightEntityPrototype = "AndroidLightMarker";
    [DataField]
    public string TogglelLightAction = "ActionToggleAndroidLeds";

    public EntityUid? ToggleLightActionEntity;
}

[Serializable, NetSerializable]
public enum AndroidVisuals : byte
{
    Light
}

[Serializable, NetSerializable]
public sealed partial class AndroidChargeDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Source;

    public AndroidChargeDoAfterEvent(NetEntity source)
    {
        Source = source;
    }
}
