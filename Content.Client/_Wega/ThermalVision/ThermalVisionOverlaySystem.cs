using Content.Client.Overlays;
using Content.Shared._Wega.ThermalVision;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._Wega.ThermalVision;

/// <summary>
/// Enables the <see cref="ThermalVisionOverlay"/> whenever the local player has a
/// <see cref="ThermalVisionComponent"/>, either directly or on a worn item.
/// </summary>
public sealed partial class ThermalVisionOverlaySystem : EquipmentHudSystem<ThermalVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ThermalVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ThermalVisionOverlay(EntityManager);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ThermalVisionComponent> component)
    {
        base.UpdateInternal(component);

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
