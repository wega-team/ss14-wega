using Content.Shared.CCVar;
using Content.Shared.Speech.Synthesis;
using Content.Shared.Speech.Synthesis.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Content.Shared.Chat;

namespace Content.Server.Speech.Synthesis.System;

/// <summary>
/// Обрабатывает барки для сущностей.
/// </summary>
public sealed partial class BarkSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    [SubscribeLocalEvent]
    private void OnEntitySpoke(EntityUid uid, SpeechSynthesisComponent comp, EntitySpokeEvent args)
    {
        if (comp.VoicePrototypeId is null || !ProtoMan.TryIndex(comp.VoicePrototypeId, out var barkProto)
           || !_cfg.GetCVar(WegaCVars.BarksEnabled))
            return;

        var isObfuscated = args.ObfuscatedMessage != null;
        var sourceEntity = GetNetEntity(uid);

        var soundPath = barkProto.SoundFiles[new Random().Next(barkProto.SoundFiles.Count)];
        RaiseNetworkEvent(new PlayBarkEvent(soundPath, sourceEntity, args.Message, comp.PlaybackSpeed, isObfuscated));
    }

    [SubscribeNetworkEvent]
    private void OnRequestPreviewBark(RequestPreviewBarkEvent ev, EntitySessionEventArgs args)
    {
        if (string.IsNullOrEmpty(ev.BarkVoiceId) || !ProtoMan.TryIndex<BarkPrototype>(ev.BarkVoiceId, out var barkProto)
            || !_cfg.GetCVar(WegaCVars.BarksEnabled))
            return;

        var soundPath = barkProto.SoundFiles[new Random().Next(barkProto.SoundFiles.Count)];
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
