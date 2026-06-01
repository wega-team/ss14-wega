using Content.Client.Ninja.Overlays;
using Content.Shared.Ninja.Components;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Shows a full-screen animation and plays a looping ambient sound while the local player
/// is waiting for their ninja clone to spawn.
/// </summary>
public sealed class NinjaCloningOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private NinjaCloningOverlay _overlay = default!;
    private EntityUid? _audioStream;

    private static readonly SoundSpecifier CapsuleSound =
        new SoundPathSpecifier("/Audio/Machines/scan_loop.ogg",
            AudioParams.Default.WithVolume(-10f).WithLoop(true));

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<NinjaCloningComponent, AfterAutoHandleStateEvent>(OnStateUpdated);
        SubscribeLocalEvent<NinjaCloningComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NinjaCloningComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NinjaCloningComponent, ComponentShutdown>(OnShutdown);
    }

    private void ShowOverlay()
    {
        _overlayMan.AddOverlay(_overlay);
        _audioStream ??= _audio.PlayGlobal(CapsuleSound, Filter.Local(), false)?.Entity;
    }

    private void HideOverlay()
    {
        _overlayMan.RemoveOverlay(_overlay);
        StopSound();
    }

    private void StopSound()
    {
        if (_audioStream is not { } stream)
            return;

        // Mute immediately so there is no 1-tick tail while QueueDel propagates.
        if (TryComp<AudioComponent>(stream, out var audio))
            _audio.SetGain(stream, 0f, audio);

        _audioStream = _audio.Stop(stream);
    }

    private void OnStateUpdated(EntityUid uid, NinjaCloningComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity != uid)
            return;

        if (comp.InCapsule)
            ShowOverlay();
        else
            HideOverlay();
    }

    private void OnPlayerAttached(EntityUid uid, NinjaCloningComponent comp, LocalPlayerAttachedEvent args)
    {
        if (comp.InCapsule)
            ShowOverlay();
    }

    private void OnPlayerDetached(EntityUid uid, NinjaCloningComponent comp, LocalPlayerDetachedEvent args)
    {
        HideOverlay();
    }

    private void OnShutdown(EntityUid uid, NinjaCloningComponent comp, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            HideOverlay();
    }
}
