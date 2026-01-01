using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;
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
public sealed class BloodMagicSelectSpellMessage(EntProtoId spell) : EuiMessageBase
{
    public readonly EntProtoId Spell = spell;
}

[Serializable, NetSerializable]
public sealed class BloodRitesSelectRitesMessage : BoundUserInterfaceMessage
{
    public EntProtoId Rites { get; }

    public BloodRitesSelectRitesMessage(EntProtoId rites)
    {
        Rites = rites;
    }
}

[Serializable, NetSerializable]
public sealed class BloodConstructSelectMessage : BoundUserInterfaceMessage
{
    public EntProtoId Construct { get; }

    public BloodConstructSelectMessage(EntProtoId construct)
    {
        Construct = construct;
    }
}

[Serializable, NetSerializable]
public sealed class BloodStructureSelectMessage : BoundUserInterfaceMessage
{
    public EntProtoId Item { get; }

    public BloodStructureSelectMessage(EntProtoId item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable]
public sealed class SelectBloodRuneMessage : BoundUserInterfaceMessage
{
    public EntProtoId RuneProtoId { get; }

    public SelectBloodRuneMessage(EntProtoId runeProtoId)
    {
        RuneProtoId = runeProtoId;
    }
}

[Serializable, NetSerializable]
public sealed class EmpoweringRuneSelectSpellMessage : BoundUserInterfaceMessage
{
    public EntProtoId Spell { get; }

    public EmpoweringRuneSelectSpellMessage(EntProtoId spell)
    {
        Spell = spell;
    }
}

[Serializable, NetSerializable]
public sealed class SummoningRuneSelectCultistMessage : BoundUserInterfaceMessage
{
    public NetEntity CultistUid { get; }

    public SummoningRuneSelectCultistMessage(NetEntity cultistUid)
    {
        CultistUid = cultistUid;
    }
}

[Serializable, NetSerializable]
public sealed partial class BloodMagicDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId SelectedSpell { get; }

    public BloodMagicDoAfterEvent(EntProtoId selectedSpell)
    {
        SelectedSpell = selectedSpell;
    }
}

[Serializable, NetSerializable]
public sealed partial class TeleportSpellDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EmpoweringDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId SelectedSpell { get; }

    public EmpoweringDoAfterEvent(EntProtoId selectedSpell)
    {
        SelectedSpell = selectedSpell;
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

// Abilities
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
