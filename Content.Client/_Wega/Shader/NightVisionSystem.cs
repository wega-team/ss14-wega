using Content.Client.Overlays;
using Content.Shared.Inventory.Events;
using Content.Shared.Shaders;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace Content.Client.Shaders.Systems;

public sealed partial class NightVisionSystem : ToggleableEquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private ILightManager _lightManager = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
		base.Initialize();
		SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
        _overlay = new();
    }

	public void OnHandleState(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        base.UpdateInternal(args);

        if (args.Components.Count == 0)
            return;

        var component = args.Components[0];

        _overlay.Brightness = component.Brightness;
        _overlay.LuminanceThreshold = component.LuminanceThreshold;
        _overlay.NoiseAmount = component.NoiseAmount;
        _overlay.Tint = component.Tint;

        _lightManager.DrawLighting = false;
        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _lightManager.DrawLighting = true;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
