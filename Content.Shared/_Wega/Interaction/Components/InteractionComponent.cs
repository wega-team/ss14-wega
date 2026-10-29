using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

[Access(typeof(InteractionActionSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class InteractionActionsComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<InteractionActionPrototype>, TimeSpan> Cooldowns = new();
}
