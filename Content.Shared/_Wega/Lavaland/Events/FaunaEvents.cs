using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Events;

// Legion
public sealed partial class LegionSummonSkullAction : EntityTargetActionEvent
{
    /// <summary>
    /// The entity that must be invoked.
    /// </summary>
    [DataField]
    public EntProtoId EntityId = "MobLegionSkull";

    /// <summary>
    /// How many entities should be invoked.
    /// </summary>
    [DataField]
    public int MaxSpawns = 1;
}

// Ice Demon
public sealed partial class IceDemonIceShotActionEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId BoltPrototype = "ProjectileIceDemonBolt";
}

public sealed partial class IceDemonTeleportActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float TeleportRadius = 5f;

    [DataField]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
}

// Ice Whelp
public sealed partial class IceWhelpLineBreathActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int LineLength = 8;

    [DataField]
    public EntProtoId BoltPrototype = "ProjectileIceWhelpBolt";
}

public sealed partial class IceWhelpCircleBreathActionEvent : EntityTargetActionEvent
{
    [DataField]
    public int Radius = 3;

    [DataField]
    public int ProjectileCount = 12;

    [DataField]
    public EntProtoId BoltPrototype = "ProjectileIceWhelpBolt";
}
