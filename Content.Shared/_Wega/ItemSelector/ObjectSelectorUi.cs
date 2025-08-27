using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Item.Selector.UI;

[Serializable, NetSerializable]
public enum ObjectSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ObjectSelectorUserMessage : BoundUserInterfaceMessage
{
    public List<EntProtoId> Objects;

    public ObjectSelectorUserMessage(List<EntProtoId> objects)
    {
        Objects = objects;
    }
}

[Serializable, NetSerializable]
public sealed class ObjectSelectorSelectionMessage : BoundUserInterfaceMessage
{
    public EntProtoId SelectedId;

    public ObjectSelectorSelectionMessage(EntProtoId selectedId)
    {
        SelectedId = selectedId;
    }
}
