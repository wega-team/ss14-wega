using Content.Client.Ninja.Overlays;
using Content.Shared.Ninja.Components;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Shows a full-screen animation and plays a periodic ambient sound while the local player
/// is waiting for their ninja clone to spawn.
/// </summary>
public sealed class NinjaCloningOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private NinjaCloningOverlay _overlay = default!;

    private bool _active;

    private static readonly SoundSpecifier CapsuleSound =
        new SoundPathSpecifier("/Audio/_Wega/Effects/ha.ogg",
            AudioParams.Default.WithVolume(-10f));

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<NinjaCloningComponent, AfterAutoHandleStateEvent>(OnStateUpdated);
        SubscribeLocalEvent<NinjaCloningComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NinjaCloningComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NinjaCloningComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_active)
            return;

        // Play one "ha" each time the overlay animation completes a loop, keeping them in sync.
        if (_overlay.ConsumeLooped())
            _audio.PlayGlobal(CapsuleSound, Filter.Local(), false);
    }

    private void ShowOverlay()
    {
        _overlayMan.AddOverlay(_overlay);

        if (!_active)
        {
            _active = true;
            // Play the first one right away; subsequent ones fire on each animation loop.
            _audio.PlayGlobal(CapsuleSound, Filter.Local(), false);
            _overlay.ConsumeLooped(); // clear any pending loop flag so we don't double-play
        }
    }

    private void HideOverlay()
    {
        _overlayMan.RemoveOverlay(_overlay);
        _active = false;
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
