using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings;

[Serializable, NetSerializable]
public enum MarkingTypes : byte
{
    Base,
    NonGenetics
}
