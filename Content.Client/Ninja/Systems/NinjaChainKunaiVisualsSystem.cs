using Robust.Client.Graphics;

namespace Content.Client.Ninja.Systems;

public sealed partial class NinjaChainKunaiVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ChainKunaiChainOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ChainKunaiChainOverlay>();
    }
}
