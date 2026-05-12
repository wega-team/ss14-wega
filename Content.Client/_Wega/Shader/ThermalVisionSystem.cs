using Content.Shared.Inventory;
using Content.Shared.Thermal.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;
using Robust.Client.Graphics; //port only with permission of the code creator

namespace Content.Client.Thermal.EntitySystems;

public sealed class ThermalVisionFovSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private bool _originalDrawFov = true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, GotEquippedEvent>(OnThermalEquipped);
        SubscribeLocalEvent<ThermalVisionComponent, GotUnequippedEvent>(OnThermalUnequipped);
    }

    private void OnThermalEquipped(EntityUid uid, ThermalVisionComponent component, GotEquippedEvent args)
    {
        _originalDrawFov = _eyeManager.CurrentEye.DrawFov;
        _eyeManager.CurrentEye.DrawFov = false;
    }

    private void OnThermalUnequipped(EntityUid uid, ThermalVisionComponent component, GotUnequippedEvent args)
    {
        _eyeManager.CurrentEye.DrawFov = _originalDrawFov;
    }
}
