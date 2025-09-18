using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class RodOfAsclepiusComponent : Component
{
    [DataField]
    public EntityUid? BoundTo;

    [DataField]
    public float HealRadius = 3f;

    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan NextHealTime = TimeSpan.Zero;

    [DataField]
    public float HealAmount = 2f;

    [DataField]
    public bool HealOthers = true;
}

[Serializable, NetSerializable]
public sealed partial class RodOathDoAfterEvent : SimpleDoAfterEvent
{
}
