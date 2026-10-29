using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland.Events;

[Serializable, NetSerializable]
public sealed partial class ResurrectionCrystalAction : SimpleDoAfterEvent { }
