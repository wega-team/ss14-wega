using Content.Client.Audio;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Speech.Synthesis;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Speech.Synthesis.System;

/// <summary>
/// Система отвечающая за прогрышь звука для каждого калиента
/// </summary>
public sealed partial class BarkSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;

    private const float MinimalVolume = -10f;
    private const float WhisperFade = 4f;

    public void RequestPreviewBark(string barkVoiceId)
    {
        RaiseNetworkEvent(new RequestPreviewBarkEvent(barkVoiceId));
    }

    [SubscribeNetworkEvent]
    private void OnPlayBark(PlayBarkEvent ev)
    {
        var sourceEntity = GetEntity(ev.SourceUid);
        if (!Exists(sourceEntity) || Deleted(sourceEntity) || !HasComp<TransformComponent>(sourceEntity))
            return;

        var volumeMultiplier = 1f;
        if (_player.LocalEntity != null && HasComp<TransformComponent>(_player.LocalEntity.Value))
        {
            var sourceTransform = Transform(sourceEntity);
            var playerTransform = Transform(_player.LocalEntity.Value);

            if (sourceTransform.Coordinates.TryDistance(EntityManager, playerTransform.Coordinates, out var distance)
                && distance > SharedChatSystem.VoiceRange)
                return;
        }

        var userVolume = _cfg.GetCVar(WegaCVars.BarksVolume);
        var baseVolume = SharedAudioSystem.GainToVolume(userVolume * ContentAudioSystem.BarksMultiplier);

        var volume = MinimalVolume + baseVolume * volumeMultiplier;
        if (ev.Obfuscated) volume -= WhisperFade;

        var audioParams = new AudioParams
        {
            Volume = volume,
            Variation = 0.125f
        };

        var messageLength = ev.Message.Length;
        var totalDuration = messageLength * 0.05f;
        var soundInterval = 0.15f / ev.PlaybackSpeed;

        var soundCount = (int)(totalDuration / soundInterval);
        soundCount = Math.Max(soundCount, 1);

        var audioResource = new AudioResource();
        audioResource.Load(IoCManager.Instance!, new ResPath(ev.SoundPath));

        var soundSpecifier = new ResolvedPathSpecifier(ev.SoundPath);

        for (int i = 0; i < soundCount; i++)
        {
            Timer.Spawn(TimeSpan.FromSeconds(i * soundInterval),
                () =>
                {
                    if (!Exists(sourceEntity) || Deleted(sourceEntity))
                        return;

                    _audio.PlayEntity(audioResource.AudioStream, sourceEntity, soundSpecifier, audioParams);
                });
        }
    }
}
