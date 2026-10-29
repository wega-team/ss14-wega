using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public enum VampireClassEnum : byte
{
    NonSelected = 0,
    Hemomancer,
    Umbrae,
    Gargantua,
    Dantalion,
    Bestia // Here! Don't bite me!
}

[Serializable, NetSerializable]
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
    Digit4
}

// Bestia enum
[Serializable, NetSerializable]
public enum BestiaOrganType : byte
{
    Unknown = 0,
    Heart,
    Lungs,
    Liver,
    Kidneys,
    Eyes,
    Stomach
}
