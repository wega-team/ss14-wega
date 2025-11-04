using System.Numerics;
using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Shaders.Systems;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly ProtoId<ShaderPrototype> NightVision = "NightVision";
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _baseShader;
    private ShaderInstance? _currentShader;

    public float Brightness { get; set; } = 2.5f;
    public float LuminanceThreshold { get; set; } = 0.8f;
    public float NoiseAmount { get; set; } = 0.3f;
    public Color Tint;

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _baseShader = _prototypeManager.Index(NightVision).Instance();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { } player
            || !_entityManager.TryGetComponent(player, out EyeComponent? eye)
            || args.Viewport.Eye != eye.Eye)
        {
            return false;
        }

        if (_entityManager.TryGetComponent<NightVisionComponent>(player, out var nv))
        {
            Brightness = nv.Brightness;
            LuminanceThreshold = nv.LuminanceThreshold;
            NoiseAmount = nv.NoiseAmount;
            Tint = nv.Tint;

            _currentShader = _baseShader.Duplicate();
            return true;
        }

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _currentShader == null)
            return;

        var handle = args.WorldHandle;

        _currentShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _currentShader.SetParameter("tint", new Vector3(Tint.R, Tint.G, Tint.B));
        _currentShader.SetParameter("brightness", Brightness);
        _currentShader.SetParameter("luminance_threshold", LuminanceThreshold);
        _currentShader.SetParameter("noise_amount", NoiseAmount);
        _currentShader.SetParameter("TIME", (float)_gameTiming.RealTime.TotalSeconds);

        handle.UseShader(_currentShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
