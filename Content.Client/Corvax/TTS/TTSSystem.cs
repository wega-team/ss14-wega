using Content.Shared.Chat;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.SoundInsolation; // Corvax-Wega-SoundInsolation
using Robust.Client.Audio;
using Robust.Client.Player; // Corvax-Wega-SoundInsolation
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!; // Corvax-Wega-SoundInsolation
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SoundInsulationSystem _soundInsulation = default!; // Corvax-Wega-SoundInsolation

    private ISawmill _sawmill = default!;
    private static MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    /// <summary>
    /// The volume at which the TTS sound will not be heard.
    /// </summary>
    private const float MinimalVolume = -10f;

    private float _volume = 0.0f;
    private int _fileIdx = 0;

    public override void Initialize()
    {
        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, _contentRoot);
        }

        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Verbose($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        // Corvax-Wega-SoundInsolation-start
        float volumeMultiplier = 1f;
        if (ev.SourceUid != null && _player.LocalEntity != null)
        {
            var sourceUid = GetEntity(ev.SourceUid.Value);
            var insulation = _soundInsulation.GetSoundInsulation(sourceUid, _player.LocalEntity.Value);
            if (insulation >= 0.95f)
            {
                _contentRoot.RemoveFile(filePath);
                return;
            }

            volumeMultiplier = 1f - insulation;
        }
        // Corvax-Wega-SoundInsolation-end

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, Prefix / filePath);

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(ev.IsWhisper, volumeMultiplier)) // Corvax-Wega-SoundInsolation-Edit
            .WithMaxDistance(AdjustDistance(ev.IsWhisper));

        var soundSpecifier = new ResolvedPathSpecifier(Prefix / filePath);

        if (ev.SourceUid != null)
        {
            if (!TryGetEntity(ev.SourceUid.Value, out _))
                return;
            var sourceUid = GetEntity(ev.SourceUid.Value);
            _audio.PlayEntity(audioResource.AudioStream, sourceUid, soundSpecifier, audioParams);
        }
        else
        {
            _audio.PlayGlobal(audioResource.AudioStream, soundSpecifier, audioParams);
        }

        _contentRoot.RemoveFile(filePath);
    }

    private float AdjustVolume(bool isWhisper, float volumeMultiplier) // Corvax-Wega-SoundInsolation-Edit
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);
        volume *= volumeMultiplier; // Corvax-Wega-SoundInsolation

        if (isWhisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
