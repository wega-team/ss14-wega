using Content.Shared.Chat.Prototypes;
using Content.Shared.Corvax.TTS;
using Content.Shared.Genetics.Systems; // Corvax-Wega-Genetics
using Content.Shared.Height; // Corvax-Wega-Height
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Synthesis; // Corvax-Wega-Barks
using Content.Shared.Speech.Components;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Dictates what species and age this character "looks like"
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem), typeof(SharedDnaModifierSystem), typeof(HeightSystem))] // Corvax-Wega-Edit
public sealed partial class HumanoidProfileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender Gender;

    /// <summary>
    /// Holds the EmoteSoundsPrototype that the humanoid will use to speak with
    /// To change in-game, you still have to use the <see cref="VoiceChangedEvent"/>
    /// or edit the <see cref="VocalComponent"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype> Voice = HumanoidCharacterProfile.DefaultVoice;

    [DataField, AutoNetworkedField]
    public Sex Sex;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidCharacterProfile.DefaultSpecies;

    // Corvax-Wega-start
    [DataField, AutoNetworkedField]
    public Status Status = Status.No;

    [DataField, AutoNetworkedField]
    public float Height = 175.0f;

    [DataField("barkvoice")]
    public ProtoId<BarkPrototype> BarkVoice { get; set; } = HumanoidProfileSystem.DefaultBarkVoice;
    // Corvax-Wega-end

    // Corvax-TTS-Start
    [DataField, AutoNetworkedField]
    public ProtoId<TTSVoicePrototype> TTSVoice { get; set; } = HumanoidProfileSystem.DefaultVoice;
    // Corvax-TTS-End
}
