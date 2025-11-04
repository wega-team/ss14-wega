using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Prototype("internalDamage")]
public sealed partial class InternalDamagePrototype : IPrototype, ISerializationHooks
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = default!;

    [DataField("category", required: true)]
    public DamageCategory Category { get; private set; }

    [DataField("blacklistPart")]
    public List<string>? BlacklistPart { get; private set; }

    [DataField("blacklistSpecies")]
    public List<ProtoId<SpeciesPrototype>>? BlacklistSpecies { get; private set; } = new();

    [DataField("supportedTypes", required: true)]
    public List<string> SupportedTypes { get; private set; } = new();

    [DataField("bodyVisuals")]
    public string? BodyVisuals { get; private set; }

    [DataField]
    public float MinDamage = 5f;

    [DataField]
    public float Severity = 1f;

    [DataField]
    public float Chance = 0.05f;
}

[Serializable, NetSerializable]
public enum DamageCategory : byte
{
    PhysicalTrauma,
    Burns,
    Fractures,
    InternalBleeding,
    CriticalBurns,
    ForeignObjects
}
