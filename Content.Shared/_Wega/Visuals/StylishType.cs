using Robust.Shared.Serialization;

namespace Content.Shared.Visuals;

/// <summary>
/// Determines which texture style format will be used.
/// </summary>
[Serializable, NetSerializable]
public enum StylishType : int
{
    /// <summary>
    /// Do not use this key anywhere.
    /// It is ignored by the logic for the other.
    /// </summary>
    Current = 0,
    Old = 1,
    Event = 2,
}
