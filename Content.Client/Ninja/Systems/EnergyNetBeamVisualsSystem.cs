using Robust.Client.Graphics;

namespace Content.Client.Ninja.Systems;

public sealed partial class EnergyNetBeamVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new EnergyNetBeamOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<EnergyNetBeamOverlay>();
    }
}
