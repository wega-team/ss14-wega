using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Blood.Cult;

// Events
public sealed class GodCalledEvent : EntityEventArgs
{
}

public sealed class RitualConductedEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class BloodMagicPressedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public BloodMagicPressedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class BloodMagicMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string SelectedSpell { get; }

    public BloodMagicMenuClosedEvent(NetEntity uid, string selectedSpell)
    {
        Uid = uid;
        SelectedSpell = selectedSpell;
    }
}

[Serializable, NetSerializable]
public sealed partial class BloodMagicDoAfterEvent : SimpleDoAfterEvent
{
    public string SelectedSpell { get; }

    public BloodMagicDoAfterEvent(string selectedSpell)
    {
        SelectedSpell = selectedSpell;
    }
}

[Serializable, NetSerializable]
public sealed partial class TeleportSpellDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class EmpoweringRuneMenuOpenedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public EmpoweringRuneMenuOpenedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class EmpoweringRuneMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string SelectedSpell { get; }

    public EmpoweringRuneMenuClosedEvent(NetEntity uid, string selectedSpell)
    {
        Uid = uid;
        SelectedSpell = selectedSpell;
    }
}

[Serializable, NetSerializable]
public sealed partial class EmpoweringDoAfterEvent : SimpleDoAfterEvent
{
    public string SelectedSpell { get; }

    public EmpoweringDoAfterEvent(string selectedSpell)
    {
        SelectedSpell = selectedSpell;
    }
}

[Serializable, NetSerializable]
public sealed class BloodRitesPressedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public BloodRitesPressedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class BloodRitesMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string SelectedRites { get; }

    public BloodRitesMenuClosedEvent(NetEntity uid, string selectedRites)
    {
        Uid = uid;
        SelectedRites = selectedRites;
    }
}

[Serializable, NetSerializable]
public sealed class RunesMenuOpenedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public RunesMenuOpenedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class RuneSelectEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string RuneProto { get; }

    public RuneSelectEvent(NetEntity uid, string runeProto)
    {
        Uid = uid;
        RuneProto = runeProto;
    }
}

[Serializable, NetSerializable]
public sealed partial class BloodRuneDoAfterEvent : SimpleDoAfterEvent
{
    public string SelectedRune { get; }
    public NetEntity Rune { get; }

    public BloodRuneDoAfterEvent(string selectedRune, NetEntity rune)
    {
        SelectedRune = selectedRune;
        Rune = rune;
    }
}

[Serializable, NetSerializable]
public sealed partial class BloodRuneCleaningDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed class SummoningRuneMenuOpenedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public SummoningRuneMenuOpenedEvent(NetEntity uid)
    {
        Uid = uid;
    }
}

[Serializable, NetSerializable]
public sealed class SummoningSelectedEvent : EntityEventArgs
{
    public NetEntity User { get; }
    public NetEntity Target { get; }

    public SummoningSelectedEvent(NetEntity user, NetEntity target)
    {
        User = user;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class OpenConstructMenuEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public NetEntity ConstructUid { get; }
    public NetEntity Mind { get; }

    public OpenConstructMenuEvent(NetEntity uid, NetEntity constructUid, NetEntity mind)
    {
        Uid = uid;
        ConstructUid = constructUid;
        Mind = mind;
    }
}

[Serializable, NetSerializable]
public sealed class BloodConstructMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public NetEntity ConstructUid { get; }
    public NetEntity Mind { get; }
    public string ConstructProto { get; }

    public BloodConstructMenuClosedEvent(NetEntity uid, NetEntity constructUid, NetEntity mind, string constructProto)
    {
        Uid = uid;
        ConstructUid = constructUid;
        Mind = mind;
        ConstructProto = constructProto;
    }
}

[Serializable, NetSerializable]
public sealed class OpenStructureMenuEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public NetEntity Structure { get; }

    public OpenStructureMenuEvent(NetEntity uid, NetEntity structure)
    {
        Uid = uid;
        Structure = structure;
    }
}

[Serializable, NetSerializable]
public sealed class BloodStructureMenuClosedEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public string Item { get; }
    public NetEntity Structure { get; }

    public BloodStructureMenuClosedEvent(NetEntity uid, string item, NetEntity structure)
    {
        Uid = uid;
        Item = item;
        Structure = structure;
    }
}

// Abilities
public sealed partial class BloodCultObjectiveActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultCommuneActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultBloodMagicActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultStunActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultTeleportActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultElectromagneticPulseActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultShadowShacklesActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultTwistedConstructionActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultSummonEquipmentActionEvent : InstantActionEvent
{
}
public sealed partial class BloodCultSummonDaggerActionEvent : InstantActionEvent
{
}

public sealed partial class RecallBloodDaggerEvent : InstantActionEvent
{
}


public sealed partial class BloodCultHallucinationsActionEvent : EntityTargetActionEvent
{
}

public sealed partial class BloodCultConcealPresenceActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultBloodRitesActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultBloodOrbActionEvent : InstantActionEvent
{
}

public sealed partial class BloodCultBloodRechargeActionEvent : EntityTargetActionEvent
{
}

public sealed partial class BloodCultBloodSpearActionEvent : InstantActionEvent
{
}

public sealed partial class RecallBloodSpearEvent : InstantActionEvent
{
}

public sealed partial class BloodCultBloodBoltBarrageActionEvent : InstantActionEvent
{
}
