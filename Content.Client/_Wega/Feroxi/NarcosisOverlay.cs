using Content.Shared.CCVar;
using Content.Shared.Feroxi;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Wega.Feroxi;

public sealed partial class NarcosisOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "FeroxiNarcosis";

    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly StatusEffectsSystem _statusEffects;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public float Strength;
    private float _threshold = 0.6f;
    private float _rampTime = 4f;

    public NarcosisOverlay()
    {
        IoCManager.InjectDependencies(this);

        _statusEffects = _entity.System<StatusEffectsSystem>();
        _shader = _prototype.Index(Shader).InstanceUnique();

        ZIndex = 8;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        var target = 0f;

        foreach (var effect in _statusEffects.EnumerateStatusEffects<NarcosisStatusEffectComponent>(player))
        {
            var narcosis = effect.Comp2;
            var timeLeft = effect.Comp1.EndEffectTime is { } end
                ? (float) (end - _timing.CurTime).TotalSeconds
                : narcosis.FullTime;

            target = MathF.Max(target, Math.Clamp(timeLeft / MathF.Max(narcosis.FullTime, 0.01f), 0f, 1f));
            _rampTime = narcosis.RampTime;
            _threshold = narcosis.Threshold;
        }

        Strength += (target - Strength) * Math.Min(1f, args.DeltaSeconds / MathF.Max(_rampTime, 0.01f));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_config.GetCVar(CCVars.DisableRainbowOverlay))
            return false;

        if (!_entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) || args.Viewport.Eye != eye.Eye)
            return false;

        return Strength > 0.01f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("strength", Strength);
        _shader.SetParameter("threshold", _threshold);
        _shader.SetParameter("motion", _config.GetCVar(CCVars.ReducedMotion) ? 0f : 1f);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
