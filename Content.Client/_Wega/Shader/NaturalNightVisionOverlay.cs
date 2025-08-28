using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Shaders.Systems;

public sealed class NaturalNightVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _nightVisionShader;

    public float BrightnessGain = 3.0f;
    public float Contrast = 1.3f;
    public Color TintColor = Color.FromHex("#1c89f2");
    public float VisionRadius = 6.0f;

    public NaturalNightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index<ShaderPrototype>("NaturalNightVision").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_entityManager.TryGetComponent<NaturalNightVisionComponent>(_playerManager.LocalEntity, out var naturalNight)
            || !naturalNight.Visible)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter("brightness_gain", BrightnessGain);
        _nightVisionShader.SetParameter("vision_contrast", Contrast);
        _nightVisionShader.SetParameter("tint_color", TintColor);
        _nightVisionShader.SetParameter("vision_radius", VisionRadius);

        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
