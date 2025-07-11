using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology.Events;

[Serializable, NetSerializable]
public sealed partial class SlimeFeedDoAfterEvent : SimpleDoAfterEvent
{
    public float Hunger;
}
