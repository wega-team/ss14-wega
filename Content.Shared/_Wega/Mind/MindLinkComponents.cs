using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mind;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindLinkComponent : Component
{
    /// <summary>
    /// Available mind channels for this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<MindChannelPrototype>> Channels = new();
}

[RegisterComponent]
public sealed partial class AdminMindLinkListenerComponent : Component;
