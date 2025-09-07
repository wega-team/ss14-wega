using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Item.Selector.UI;

[Serializable, NetSerializable]
public enum ItemSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ItemSelectorUserMessage : BoundUserInterfaceMessage
{
    public List<EntProtoId> Items;

    public ItemSelectorUserMessage(List<EntProtoId> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class ItemSelectorSelectionMessage : BoundUserInterfaceMessage
{
    public NetEntity User;
    public EntProtoId SelectedId;

    public ItemSelectorSelectionMessage(NetEntity user, EntProtoId selectedId)
    {
        User = user;
        SelectedId = selectedId;
    }
}
