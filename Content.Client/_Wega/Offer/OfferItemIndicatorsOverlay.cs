using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Offer;

public sealed class OfferItemIndicatorsOverlay : Overlay
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly Texture _indicatorTexture;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public OfferItemIndicatorsOverlay()
    {
        IoCManager.InjectDependencies(this);

        var resourceCache = IoCManager.Resolve<IResourceCache>();
        _indicatorTexture = resourceCache.GetTexture("/Textures/_Wega/Interface/Misc/give_item.rsi/give_item.png");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        var screen = args.ScreenHandle;
        var mousePos = _inputManager.MouseScreenPosition.Position;

        screen.DrawTexture(_indicatorTexture, mousePos - _indicatorTexture.Size / 2, Color.White);
    }
}
