using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction;

[Serializable, NetSerializable]
public sealed partial class InteractionDoAfterEvent : SimpleDoAfterEvent
{
    public readonly ProtoId<InteractionActionPrototype> ActionId;

    public InteractionDoAfterEvent(ProtoId<InteractionActionPrototype> actionId)
    {
        ActionId = actionId;
    }
}
