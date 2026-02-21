using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland;

[Serializable, NetSerializable]
public enum UtilityVendorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CategoryData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Priority { get; set; }
    public Dictionary<EntProtoId, FixedPoint2> Inventory { get; set; }

    public CategoryData(string id, string name, int priority, Dictionary<EntProtoId, FixedPoint2> inventory)
    {
        Id = id;
        Name = name;
        Priority = priority;
        Inventory = inventory;
    }
}

[Serializable, NetSerializable]
public sealed class UtilityVendorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly FixedPoint2 Points;
    public readonly List<CategoryData> Categories;

    public UtilityVendorBoundUserInterfaceState(FixedPoint2 points, List<CategoryData> categories)
    {
        Points = points;
        Categories = categories;
    }
}

[Serializable, NetSerializable]
public sealed class UtilityVendorPurchaseMessage : BoundUserInterfaceMessage
{
    public readonly string ItemId;

    public UtilityVendorPurchaseMessage(string itemId)
    {
        ItemId = itemId;
    }
}
