using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

[Serializable, NetSerializable]
public sealed partial class ToggleHandheldBeaconDoAfterEvent : SimpleDoAfterEvent { }
