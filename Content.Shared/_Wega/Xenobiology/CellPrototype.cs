using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology;

[Prototype]
public sealed class CellPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField]
    public LocId Name;

    [DataField]
    public string Feature = string.Empty;

    [DataField]
    public float Stability = 1;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public int Cost = 5;

    [DataField]
    public List<ProtoId<CellModifierPrototype>> Modifiers = [];
}
