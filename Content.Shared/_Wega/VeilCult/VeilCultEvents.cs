using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult;

// Events
public sealed partial class VeilCultMidasTouchActionEvent : InstantActionEvent
{
}

public sealed partial class VeilCultMidasTouchGetHandEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EnchantingDoAfterEvent : SimpleDoAfterEvent
{
	public EntProtoId Entity;
}


public sealed partial class CrusherEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class ConfusionEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class KnockbackEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class SwordsmenEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class BloodshedEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class HasteEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class ReflectionEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class CamouflageEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class AbsorbEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class FlashEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class HardenPlatesEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class NorthStarEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class RedFlameEnchantActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class VeilCultTeleportDoAfterEvent : SimpleDoAfterEvent
{
	public NetEntity Target;
}

[Serializable, NetSerializable]
public sealed partial class MidasTouchDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class StrangeShardDoAfterEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public record struct SiliconVeilCultHackedEvent(EntityUid user);

[Serializable, NetSerializable]
public sealed class VeilBeaconNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

[Serializable, NetSerializable]
public enum TeleportEnchantUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class TeleportEnchantDestinationMessage(NetEntity netEnt, string pointName) : BoundUserInterfaceMessage
{
    public NetEntity NetEnt = netEnt;
    public string PointName = pointName;
}

[Serializable, NetSerializable]
public enum VeilBeaconUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class VeilCultBeaconComponentState(string assignedLabel) : IComponentState
{
    public string AssignedLabel = assignedLabel;

    public int MaxLabelChars;
}

