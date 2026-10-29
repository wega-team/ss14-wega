using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Lavaland;

[Prototype]
public sealed partial class UtilityVendorCategoryPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public int Priority { get; private set; } = 0;

    [DataField("inventory", customTypeSerializer: typeof(DictionarySerializer<EntProtoId, FixedPoint2>))]
    public Dictionary<EntProtoId, FixedPoint2> InventoryTemplate { get; private set; } = new();
}
