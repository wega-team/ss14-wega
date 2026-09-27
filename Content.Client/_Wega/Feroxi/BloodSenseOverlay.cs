using System.Numerics;
using Content.Shared.Feroxi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Wega.Feroxi;

public sealed partial class BloodSenseOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "BloodSense";

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public BloodSenseOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<SharedTransformSystem>();
        _shader = _prototype.Index(Shader).InstanceUnique();

        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null
            || _player.LocalEntity is not { } player
            || !_entity.TryGetComponent<BloodSenseActiveComponent>(player, out var active)
            || !_entity.TryGetComponent<BloodSenseComponent>(player, out var sense))
            return;

        var handle = args.WorldHandle;

        DrawScreen(handle, args, sense);
        DrawTargets(handle, args, sense, active);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawScreen(DrawingHandleWorld handle, in OverlayDrawArgs args, BloodSenseComponent sense)
    {
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture!);
        _shader.SetParameter("red_hue_range", sense.RedHueRange);
        _shader.SetParameter("min_saturation", sense.MinSaturation);
        _shader.SetParameter("min_value", sense.MinValue);
        _shader.SetParameter("darkness", sense.Darkness);
        _shader.SetParameter("vignette", sense.Vignette);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private void DrawTargets(DrawingHandleWorld handle, in OverlayDrawArgs args, BloodSenseComponent sense, BloodSenseActiveComponent active)
    {
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var time = (float) _timing.RealTime.TotalSeconds;
        var pulse = 1f - sense.PulseDepth * 0.5f * (1f + MathF.Sin(time * MathF.Tau / sense.PulsePeriod));

        foreach (var (target, strength) in active.Targets)
        {
            if (!_entity.TryGetComponent<TransformComponent>(target, out var xform)
                || xform.MapID != args.MapId
                || !_entity.TryGetComponent<SpriteComponent>(target, out var sprite)
                || !sprite.Visible)
                continue;

            var intensity = Math.Clamp(strength / sense.FullStrength, 0f, 1f);
            var alpha = (sense.MinAlpha + (sense.MaxAlpha - sense.MinAlpha) * intensity) * pulse;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            var originalColor = sprite.Color;

            _sprite.SetColor((target, sprite), sense.SilhouetteColor.WithAlpha(alpha));
            _sprite.RenderSprite((target, sprite), handle, eyeRotation, rotation, position);
            _sprite.SetColor((target, sprite), originalColor);
        }
    }
}
