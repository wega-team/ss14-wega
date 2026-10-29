using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult;

// Events
public sealed partial class VeilGodCalledEvent : EntityEventArgs
{
}

public sealed partial class VeilRitualConductedEvent : EntityEventArgs
{
}

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

    public EnchantingDoAfterEvent(EntProtoId entity)
    {
        Entity = entity;
    }
}

public sealed partial class CrusherEnchantActionEvent : InstantActionEvent
{
}

public sealed partial class DismantlingEnchantActionEvent : InstantActionEvent
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

public sealed partial class SmokeEnchantActionEvent : InstantActionEvent
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
    public NetEntity Beacon;

    public VeilCultTeleportDoAfterEvent(NetEntity beacon)
    {
        Beacon = beacon;
    }
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
public record struct SiliconVeilCultHackedEvent(EntityUid User);

// UI KEYS
[Serializable, NetSerializable]
public enum TeleportEnchantUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum VeilBeaconUiKey : byte
{
    Key
}

// STATES AND MESSAGES
[Serializable, NetSerializable]
public sealed partial class VeilBeaconNameBoundUserInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int MaxChars;

    public VeilBeaconNameBoundUserInterfaceState(string name, int maxChars)
    {
        Name = name;
        MaxChars = maxChars;
    }
}

[Serializable, NetSerializable]
public sealed partial class VeilBeaconNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

[Serializable, NetSerializable]
public sealed partial class TeleportEnchantDestinationMessage(NetEntity netEnt) : BoundUserInterfaceMessage
{
    public NetEntity NetEnt = netEnt;
}

[Serializable, NetSerializable]
public sealed partial class VeilCultBeaconComponentState(string assignedName, int maxNameChars) : IComponentState // <-- Эт не ui тип, а к компонентам ближе
{
    public string AssignedName = assignedName;
    public int MaxNameChars = maxNameChars;
}

[Serializable, NetSerializable]
public sealed partial class TeleportationEnchantBoundUserInterfaceState : BoundUserInterfaceState
{
    public HashSet<TeleportPoint> Warps;

    public TeleportationEnchantBoundUserInterfaceState(HashSet<TeleportPoint> warps)
    {
        Warps = warps;
    }
}
