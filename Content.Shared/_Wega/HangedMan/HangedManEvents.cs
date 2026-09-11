using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.HangedMan;

/// <summary>
/// Raised on the structure when the hanging do-after finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HangDoAfterEvent : SimpleDoAfterEvent;
