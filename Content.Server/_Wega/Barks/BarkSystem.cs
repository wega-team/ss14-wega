using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Speech.Synthesis;
using Content.Shared.Speech.Synthesis.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Speech.Synthesis.System;

/// <summary>
/// Обрабатывает барки для сущностей.
/// </summary>
public sealed partial class BarkSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;

    private bool _barksEnabled;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, WegaCVars.BarksEnabled, OnBarksEnabledChanged, true);
    }

    private void OnBarksEnabledChanged(bool value)
    {
        _barksEnabled = value;
    }

    [SubscribeLocalEvent]
    private void OnEntitySpoke(EntityUid uid, SpeechSynthesisComponent comp, EntitySpokeEvent args)
    {
        if (comp.VoicePrototypeId is null || !ProtoMan.TryIndex(comp.VoicePrototypeId, out var barkProto)
           || !_barksEnabled)
            return;

        var isObfuscated = args.ObfuscatedMessage != null;
        var sourceEntity = GetNetEntity(uid);
        var soundPath = barkProto.SoundFiles[_random.Next(barkProto.SoundFiles.Count)];

        var range = isObfuscated
            ? SharedChatSystem.WhisperMuffledRange
            : SharedChatSystem.VoiceRange;

        var filter = GetReceiversFilter(uid, range);
        if (!filter.Recipients.Any())
            return;

        var textToSend = isObfuscated ? args.ObfuscatedMessage ?? string.Empty : args.Message;
        RaiseNetworkEvent(new PlayBarkEvent(soundPath, sourceEntity, textToSend, comp.PlaybackSpeed, isObfuscated),
            filter, recordReplay: false);
    }

    private Filter GetReceiversFilter(EntityUid sourceUid, float range)
    {
        var pvs = Filter.Pvs(sourceUid);
        var filter = Filter.Empty();
        var sourceXform = Transform(sourceUid);

        foreach (var player in pvs.Recipients)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = Transform(playerEntity);
            if (transformEntity.MapID != sourceXform.MapID)
                continue;

            if (!sourceXform.Coordinates.TryDistance(EntityManager, transformEntity.Coordinates, out var distance)
                || distance >= range)
                continue;

            filter.AddPlayer(player);
        }

        return filter;
    }

    [SubscribeNetworkEvent]
    private void OnRequestPreviewBark(RequestPreviewBarkEvent ev, EntitySessionEventArgs args)
    {
        if (string.IsNullOrEmpty(ev.BarkVoiceId) || !ProtoMan.TryIndex<BarkPrototype>(ev.BarkVoiceId, out var barkProto)
            || !_cfg.GetCVar(WegaCVars.BarksEnabled))
            return;

        var soundPath = barkProto.SoundFiles[_random.Next(barkProto.SoundFiles.Count)];
        var soundSpecifier = new SoundPathSpecifier(soundPath);

        var audioParams = new AudioParams
        {
            Pitch = 1.0f,
            Volume = 4f,
            Variation = 0.125f
        };

        _audio.PlayGlobal(soundSpecifier, args.SenderSession, audioParams);
    }
}
