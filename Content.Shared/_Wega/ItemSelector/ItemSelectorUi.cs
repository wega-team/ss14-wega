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
    public List<string> Items;

    public ItemSelectorUserMessage(List<string> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public sealed class ItemSelectorSelectionMessage : BoundUserInterfaceMessage
{
    public NetEntity User;
    public string SelectedId;

    public ItemSelectorSelectionMessage(NetEntity user, string selectedId)
    {
        User = user;
        SelectedId = selectedId;
    }
}
