using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace Content.Client.Overlays;

/// <summary>
/// Enables the <see cref="ThermalVisionOverlay"/> whenever the local player has a
/// <see cref="ThermalVisionComponent"/>, either directly or on a worn item.
/// </summary>
public sealed partial class ThermalVisionSystem : ToggleableEquipmentHudSystem<ThermalVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ThermalVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
        _overlay = new ThermalVisionOverlay(EntityManager);
    }

    public void OnHandleState(Entity<ThermalVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
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
