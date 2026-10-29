using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Synthesis;

/// <summary>
/// Прототип для доступных барков.
/// </summary>
[Prototype]
public sealed partial class BarkPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название голоса.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Набор звуков, используемых для речи.
    /// </summary>
    [DataField("soundFiles", required: true)]
    public List<string> SoundFiles { get; private set; } = new();

    /// <summary>
    /// Доступен ли на старте раунда.
    /// </summary>
    [DataField("roundStart")]
    public bool RoundStart { get; private set; } = true;
}
