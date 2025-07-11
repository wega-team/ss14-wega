using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Vehicle;

public sealed class VehicleSystem : SharedVehicleSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnVehicleAppearanceChange);
    }

    private void OnVehicleAppearanceChange(EntityUid uid, VehicleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (component.HideRider
            && Appearance.TryGetData<bool>(uid, VehicleVisuals.HideRider, out var hide, args.Component)
            && TryComp<SpriteComponent>(component.LastRider, out var riderSprite))
            riderSprite.Visible = !hide;

        // First check is for the sprite itself
        if (Appearance.TryGetData<int>(uid, VehicleVisuals.DrawDepth, out var drawDepth, args.Component))
            args.Sprite.DrawDepth = drawDepth;

        // Set vehicle layer to animated or not (i.e. are the wheels turning or not)
        if (component.AutoAnimate
            && Appearance.TryGetData<bool>(uid, VehicleVisuals.AutoAnimate, out var autoAnimate, args.Component))
            args.Sprite.LayerSetAutoAnimated(VehicleVisualLayers.AutoAnimate, autoAnimate);
    }
}

public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels
    AutoAnimate,
}
