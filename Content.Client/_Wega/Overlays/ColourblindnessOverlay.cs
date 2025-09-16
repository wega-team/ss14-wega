using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Shaders.Systems;

public sealed class ColourblindnessOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _desaturationShader;

    public ColourblindnessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _desaturationShader = _prototypeManager.Index<ShaderPrototype>("Colourblindness").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalEntity;
        if (playerEntity == null || !_entityManager.TryGetComponent<ColourBlindnessComponent>(playerEntity, out var colourblindness))
            return;

        var handle = args.ScreenHandle;

        _desaturationShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _desaturationShader.SetParameter("ColorFilter", colourblindness.ColorFilter);
        _desaturationShader.SetParameter("Desaturation", colourblindness.Desaturation);
        _desaturationShader.SetParameter("ColorShift", colourblindness.ColorShift);

        handle.UseShader(_desaturationShader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);
    }
}
