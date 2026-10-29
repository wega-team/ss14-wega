using Robust.Shared.Serialization;

namespace Content.Shared.Interaction;

/// <summary>
/// Which entity to apply the condition to: user or target.
/// </summary>
[Serializable, NetSerializable]
public enum ConditionTarget : byte
{
    User,
    Target
}
