using Content.Client.Overlays;
using Content.Shared._Wega.NinjaVisor;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._Wega.NinjaVisor;

public sealed partial class NinjaVisorClientSystem : EquipmentHudSystem<NinjaVisorComponent>
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private ILightManager _lightManager = default!;

    private NinjaVisorNightVisionOverlay _nightVision = default!;
    private ThermalMobOverlay _thermal = default!;
    private NinjaVisorMode _activeMode = NinjaVisorMode.Off;

    public override void Initialize()
    {
        base.Initialize();

        _nightVision = new NinjaVisorNightVisionOverlay();
        _thermal = new ThermalMobOverlay(EntityManager);

        // Re-evaluate overlays whenever the networked mode changes
        SubscribeLocalEvent<NinjaVisorComponent, AfterAutoHandleStateEvent>(OnStateUpdated);
    }

    private void OnStateUpdated(EntityUid uid, NinjaVisorComponent comp, ref AfterAutoHandleStateEvent args)
    {
        _nightVision.TintColor = comp.NightVisionColor;
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NinjaVisorComponent> args)
    {
        base.UpdateInternal(args);

        var mode = NinjaVisorMode.Off;
        foreach (var comp in args.Components)
        {
            _nightVision.TintColor = comp.NightVisionColor;
            mode = comp.Mode;
            break;
        }

        if (mode == _activeMode)
            return;

        DeactivateMode(_activeMode);
        ActivateMode(mode);
        _activeMode = mode;
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        DeactivateMode(_activeMode);
        _activeMode = NinjaVisorMode.Off;
    }

    private void ActivateMode(NinjaVisorMode mode)
    {
        switch (mode)
        {
            case NinjaVisorMode.NightVision:
                _lightManager.DrawLighting = false;
                _overlayManager.AddOverlay(_nightVision);
                break;
            case NinjaVisorMode.Thermal:
                _overlayManager.AddOverlay(_thermal);
                break;
        }
    }

    private void DeactivateMode(NinjaVisorMode mode)
    {
        switch (mode)
        {
            case NinjaVisorMode.NightVision:
                _lightManager.DrawLighting = true;
                _overlayManager.RemoveOverlay(_nightVision);
                break;
            case NinjaVisorMode.Thermal:
                _overlayManager.RemoveOverlay(_thermal);
                break;
        }
    }
}
