using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

[Prototype]
public sealed partial class CellModifierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public LocId Name;

    [DataField]
    public Color Color;

    [DataField]
    public List<CellModifier> Modifiers { get; private set; } = [];
}
