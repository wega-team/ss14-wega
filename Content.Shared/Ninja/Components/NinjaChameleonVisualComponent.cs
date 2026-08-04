using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NinjaChameleonVisualComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid TargetUid;
}
