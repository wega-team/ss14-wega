using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Economy.SlotMachine;

[RegisterComponent]
public sealed partial class SlotMachineComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Working = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Plays = 0;

    [DataField]
    public string[] Slots = { "?", "?", "?" };

    [DataField]
    public int SpinCost = 10;

    [DataField]
    public EntityUid? User;

    public TimeSpan? SpinFinishTime;

    [ViewVariables(VVAccess.ReadOnly)] public int JackpotPrize = 50000;
    [ViewVariables(VVAccess.ReadOnly)] public int BigWinPrize = 2500;
    [ViewVariables(VVAccess.ReadOnly)] public int MediumWinPrize = 1250;
    [ViewVariables(VVAccess.ReadOnly)] public int SmallWinPrize = 50;
    [ViewVariables(VVAccess.ReadOnly)] public int TinyWinPrize = 10;

    // Sounds
    public SoundSpecifier CoinSound = new SoundCollectionSpecifier("CoinDrop");
    public SoundSpecifier RollSound = new SoundPathSpecifier("/Audio/_Wega/Machines/Roulette/roulettewheel.ogg");
    public SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/_Wega/Machines/Roulette/ding_short.ogg");
    public SoundSpecifier JackpotSound = new SoundPathSpecifier("/Audio/_Wega/Machines/Roulette/roulettejackpot.ogg");
    public SoundSpecifier FailedSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
}

[RegisterComponent]
public sealed partial class CursedSlotMachineComponent : Component
{
    [DataField] public int Uses = 0;
    [DataField] public int MaxUses = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public DamageSpecifier Damage = new DamageSpecifier()
    {
        DamageDict = { ["Blunt"] = 10, ["Heat"] = 10 }
    };

    // Sounds
    public SoundSpecifier RollSound = new SoundPathSpecifier("/Audio/_Wega/Machines/Roulette/cursed.ogg");
    public SoundSpecifier JackpotSound = new SoundPathSpecifier("/Audio/_Wega/Machines/Roulette/cursed_jackpot.ogg");
}

[Serializable, NetSerializable]
public enum SlotMachineVisuals : byte
{
    Working
}
