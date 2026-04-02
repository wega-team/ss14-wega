using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

public sealed partial class RaveOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "Rave";

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private ShaderInstance _shader;
    private float _pulseSpeed = 1.2f;
    private float _intensity = 1.2f;
    private float _grain = 0.25f;
    private float _distortion = 0.15f;
    private Color _baseColor = Color.FromHex("#ff3ce6");
    private Color _secondaryColor = Color.FromHex("#3c9eff");

    public RaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(Shader).InstanceUnique();
    }

    public void UpdateParameters(RaveOverlayComponent component)
    {
        _baseColor = component.BaseColor;
        _secondaryColor = component.SecondaryColor;
        _pulseSpeed = component.PulseSpeed;
        _intensity = component.Intensity;
        _grain = component.GrainStrength;
        _distortion = component.Distortion;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        float time = (float)_gameTiming.RealTime.TotalSeconds;

        float pulse = 0.7f + 0.3f * MathF.Sin(time * _pulseSpeed * MathF.PI * 2);
        float finalIntensity = _intensity * (0.8f + 0.4f * pulse);

        float hueShift = (MathF.Sin(time * 0.2f) + 1f) / 2f;
        Color currentColor = Color.InterpolateBetween(_baseColor, _secondaryColor, hueShift);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("time", time);
        _shader.SetParameter("color_r", currentColor.R);
        _shader.SetParameter("color_g", currentColor.G);
        _shader.SetParameter("color_b", currentColor.B);
        _shader.SetParameter("intensity", finalIntensity);
        _shader.SetParameter("grain", _grain);
        _shader.SetParameter("distortion", _distortion);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
