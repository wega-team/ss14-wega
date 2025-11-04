using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing;

[Serializable, NetSerializable]
public sealed partial class TearClothingDoAfterEvent : SimpleDoAfterEvent { }
