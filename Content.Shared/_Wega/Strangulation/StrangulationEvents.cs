using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Strangulation;

[Serializable, NetSerializable]
public sealed partial class StrangulationDoAfterEvent : SimpleDoAfterEvent { }
