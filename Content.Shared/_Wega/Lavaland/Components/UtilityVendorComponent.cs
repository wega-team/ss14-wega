using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
[Access(typeof(UtilityVendorSystem))]
public sealed partial class UtilityVendorComponent : Component
{
    [DataField("inventory"), ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntProtoId, FixedPoint2> Inventory { get; private set; } = new();

    [DataField("cardSlot")]
    public ItemSlot CardSlot { get; private set; } = new();
}