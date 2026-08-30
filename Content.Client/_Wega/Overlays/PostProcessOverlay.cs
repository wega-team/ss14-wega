using Content.Shared.CCVar;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class BasePostProcessOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> shader = "Postprocess";

    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private ILightManager _lightManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private ShaderInstance _basePostProcessShader;

    public BasePostProcessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _basePostProcessShader = _prototypeManager.Index(shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_configManager.GetCVar(WegaCVars.PostProcess))
            return false;

        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_lightManager.Enabled || !eyeComp.Eye.DrawLight)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        if (args.Viewport.Eye == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _basePostProcessShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _basePostProcessShader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);

        _basePostProcessShader.SetParameter("Zoom", args.Viewport.Eye.Zoom.X);

        worldHandle.UseShader(_basePostProcessShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}