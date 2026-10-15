using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

[Serializable, NetSerializable]
public sealed partial class SlimeSpeedDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class SlimeArmorDoAfterEvent : SimpleDoAfterEvent
{
}