using Content.Shared.Teleportation.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Teleportation;

public sealed partial class PortalSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly string AnimationKey = "portal_animation";

    [SubscribeLocalEvent]
    private void OnAppearanceChange(EntityUid uid, PortalComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<PortalVisualState>(uid, PortalVisuals.State, out var state, args.Component))
            state = PortalVisualState.Closed;

        if (!_sprite.TryGetLayer((uid, args.Sprite), PortalVisualLayers.Base, out _, false))
            return;

        switch (state)
        {
            case PortalVisualState.Closed:
                _sprite.LayerSetRsiState((uid, args.Sprite), PortalVisualLayers.Base, comp.ClosedSpriteState);
                break;

            case PortalVisualState.Opening:
                PlayAnimation(uid, comp.OpeningSpriteState, comp.OpeningAnimationTime, args.Sprite);
                break;

            case PortalVisualState.Open:
                if (_animation.HasRunningAnimation(uid, AnimationKey))
                    return;
                _sprite.LayerSetAutoAnimated((uid, args.Sprite), PortalVisualLayers.Base, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), PortalVisualLayers.Base, comp.OpenSpriteState);
                break;

            case PortalVisualState.Closing:
                PlayAnimation(uid, comp.ClosingSpriteState, comp.ClosingAnimationTime, args.Sprite);
                break;
        }
    }

    private void PlayAnimation(EntityUid uid, string state, TimeSpan time, SpriteComponent sprite)
    {
        if (_animation.HasRunningAnimation(uid, AnimationKey))
            return;

        if (!_sprite.TryGetLayer((uid, sprite), PortalVisualLayers.Base, out _, false))
            return;

        var anim = new Animation
        {
            Length = time,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = PortalVisualLayers.Base,
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame(state, 0f) },
                },
            },
        };
        _animation.Play(uid, anim, AnimationKey);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(EntityUid uid, PortalComponent comp, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.TryGetLayer((uid, sprite), PortalVisualLayers.Base, out _, false))
            return;

        if (!_appearance.TryGetData<PortalVisualState>(uid, PortalVisuals.State, out var state))
            return;

        switch (state)
        {
            case PortalVisualState.Opening:
                // opening flick finished -> settle into looping Open regardless of server state
                _sprite.LayerSetAutoAnimated((uid, sprite), PortalVisualLayers.Base, true);
                _sprite.LayerSetRsiState((uid, sprite), PortalVisualLayers.Base, comp.OpenSpriteState);
                break;

            case PortalVisualState.Closing:
                _sprite.LayerSetAutoAnimated((uid, sprite), PortalVisualLayers.Base, true);
                _sprite.LayerSetRsiState((uid, sprite), PortalVisualLayers.Base, comp.ClosedSpriteState);
                break;

            case PortalVisualState.Open:
                _sprite.LayerSetAutoAnimated((uid, sprite), PortalVisualLayers.Base, true);
                _sprite.LayerSetRsiState((uid, sprite), PortalVisualLayers.Base, comp.OpenSpriteState);
                break;

            case PortalVisualState.Closed:
                _sprite.LayerSetAutoAnimated((uid, sprite), PortalVisualLayers.Base, true);
                _sprite.LayerSetRsiState((uid, sprite), PortalVisualLayers.Base, comp.ClosedSpriteState);
                break;
        }
    }
}

public enum PortalVisualLayers : byte
{
    Base
}
