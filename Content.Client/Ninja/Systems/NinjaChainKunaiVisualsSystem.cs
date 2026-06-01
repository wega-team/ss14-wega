using Robust.Client.Graphics;

namespace Content.Client.Ninja.Systems;

public sealed class NinjaChainKunaiVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

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
