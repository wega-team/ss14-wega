using Content.Shared.Feroxi;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Wega.Feroxi;

public sealed partial class NarcosisOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private NarcosisOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new NarcosisOverlay();

        SubscribeLocalEvent<NarcosisStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_statusEffects.HasEffectComp<NarcosisStatusEffectComponent>(_player.LocalEntity))
        {
            if (!_overlayManager.HasOverlay<NarcosisOverlay>())
                _overlayManager.AddOverlay(_overlay);

            return;
        }

        if (_overlay.Strength < 0.01f)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<NarcosisStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlay.Strength = 0f;
        _overlayManager.RemoveOverlay(_overlay);
    }
}
