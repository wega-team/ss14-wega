using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class VoiceOfGodComponent : Component
{
    [DataField]
    public float GlobalCooldown = 30f;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, TimeSpan> CommandCooldowns = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastCommandUse = TimeSpan.Zero;
}

[RegisterComponent]
public sealed partial class VoiceOfGodImplantComponent : Component;

[Serializable, NetSerializable]
public enum VoiceOfGodEffect : byte
{
    Stun,
    Weaken,
    Sleep,
    Vomit,
    Silence,
    WakeUp,
    Heal,
    Damage,
    Bleed,
    Burn,
    Push,
    StandUp,
    SayName,
    SayUserName,
    KnockKnock,
    StateLaws,
    Throw,
    Sit,
    Stand,
    Salute,
    PlayDead,
    Clap,
    Honk,
    Rest
}
