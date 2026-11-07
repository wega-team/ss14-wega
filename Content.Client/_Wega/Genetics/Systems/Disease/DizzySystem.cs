using Content.Shared.Genetics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Genetics.System;

public sealed partial class DizzySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private DizzyOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(EntityUid uid, DizzyEffectComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(EntityUid uid, DizzyEffectComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.CurrentIntensity = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnInit(EntityUid uid, DizzyEffectComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, DizzyEffectComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlay.CurrentIntensity = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
