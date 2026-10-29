using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitItemModuleComponent : Component
{
    [DataField]
    public string ContainerId = "module_item";

    [DataField(required: true)]
    public string HandId = string.Empty;

    [DataField("item", required: true)]
    public EntProtoId ItemPrototype;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? HandItem;
}
