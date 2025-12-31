using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Blood.Cult;

[Serializable, NetSerializable]
public enum BloodCultGod : byte
{
    NarSi,
    Reaper,
    Kharin
}

[Serializable, NetSerializable]
public enum BloodCultSpell : byte
{
    Stun,
    Teleport,
    ShadowShackles,
    TwistedConstruction,
    SummonEquipment,
    BloodRites
}

[Serializable, NetSerializable]
public enum BloodCultRune : byte
{
    Offering,
    Teleport,
    Empowering,
    Revive,
    Barrier,
    Summoning,
    Bloodboil,
    Spiritrealm,
    Ritual,
    Default
}

[Serializable, NetSerializable]
public enum RuneColorVisuals : byte
{
    Color
}

[Serializable, NetSerializable]
public enum StoneSoulVisuals : byte
{
    HasSoul
}

[Serializable, NetSerializable]
public enum VeilShifterVisuals : byte
{
    Charged
}

[Serializable, NetSerializable]
public sealed class BloodMagicState : EuiStateBase
{
}

[Serializable, NetSerializable]
public enum BloodRitesUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum BloodConstructUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum BloodStructureUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BloodStructureBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<EntProtoId> Items;
    public BloodStructureBoundUserInterfaceState(List<EntProtoId> items)
    {
        Items = items;
    }
}

[Serializable, NetSerializable]
public enum BloodRunesUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BloodRitualBoundUserInterfaceState : BoundUserInterfaceState
{
}

[Serializable, NetSerializable]
public enum EmpoweringRuneUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum SummoningRuneUiKey : byte
{
    Key
}
