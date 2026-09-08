using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Synthesis.Components;

/// <summary>
/// Применяет звуки барков для сущности.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechSynthesisComponent : Component
{
    /// <summary>
    /// Прототип голоса для барков.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice")]
    public ProtoId<BarkPrototype>? VoicePrototypeId { get; set; }

    /// <summary>
    /// Скорость воспроизведения звука.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("playbackSpeed")]
    public float PlaybackSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Тональность звука.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pitch")]
    public float Pitch { get; set; } = 1.0f;

    /// <summary>
    /// Выразительность речи.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("expression")]
    public float Expression { get; set; } = 1.0f;
}
