using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client._Wega.Ninja.UI;

public sealed partial class NinjaEnergyDisplaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;

    private NinjaEnergyDisplayOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new NinjaEnergyDisplayOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }
}
