using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.NinjaVisor;

/// <summary>
/// Gives the ninja visor three switchable modes: flash protection, night vision, thermal sight.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NinjaVisorComponent : Component
{
    [DataField, AutoNetworkedField]
    public NinjaVisorMode Mode = NinjaVisorMode.Off;

    [DataField, AutoNetworkedField]
    public Color NightVisionColor = Color.FromHex("#00FF33");

    [DataField]
    public EntProtoId CycleAction = "ActionCycleNinjaVisor";

    [DataField, AutoNetworkedField]
    public EntityUid? CycleActionEntity;
}

[Serializable, NetSerializable]
public enum NinjaVisorMode : byte
{
    Off = 0,
    Flash = 1,
    NightVision = 2,
    Thermal = 3,
}

public sealed partial class CycleNinjaVisorEvent : InstantActionEvent;
