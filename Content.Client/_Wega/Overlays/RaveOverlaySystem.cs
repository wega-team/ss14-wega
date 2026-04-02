using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class RaveOverlaySystem : EquipmentHudSystem<RaveOverlayComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private RaveOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<RaveOverlayComponent> component)
    {
        base.UpdateInternal(component);

        _overlay.UpdateParameters(component.Components[0]);
        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
