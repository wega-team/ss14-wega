using Robust.Shared.Prototypes;

namespace Content.Shared.Pain;

[Prototype("painProfile")]
public sealed class PainProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("painTypes")]
    public Dictionary<string, PainLevel> PainTypes = new();
}

[DataDefinition]
public sealed partial class PainLevel
{
    [DataField("type")]
    public string Type = "Generic";

    [DataField("current")]
    public float CurrentLevel;

    [DataField("decayRate")]
    public float DecayRate = 0.5f;

    [DataField("effects")]
    public List<PainEffect> Effects = new();
}

[DataDefinition]
public sealed partial class PainEffect
{
    [DataField("threshold")]
    public float Threshold;

    [DataField("effect")]
    public PainEffectType Effect;

    [DataField("message")]
    public string? Message;
}

public enum PainEffectType
{
    Emote,
    Popup,
    MovementPenalty,
    DropItem,
    Stun,
    Vomit,
    Twitch
}
