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
public sealed class UtilityVendorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly FixedPoint2 Points;
    public readonly Dictionary<EntProtoId, FixedPoint2> Inventory;

    public UtilityVendorBoundUserInterfaceState(FixedPoint2 points, Dictionary<EntProtoId, FixedPoint2> inventory)
    {
        Points = points;
        Inventory = inventory;
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
