using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.AutoDust;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoDustComponent : Component
{
    [DataField, AutoNetworkedField]
    public AutoDustTrigger Trigger = AutoDustTrigger.OnDeath;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}

[Serializable, NetSerializable]
public enum AutoDustTrigger : byte
{
    Disabled = 0,
    OnDeath  = 1,
    OnCrit   = 2,
}

public sealed partial class ToggleAutoDustEvent : InstantActionEvent;
