using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Vehicle;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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
            && HasComp<SpriteComponent>(component.LastRider))
            _sprite.SetVisible(component.LastRider.Value, !hide);

        // First check is for the sprite itself
        if (Appearance.TryGetData<int>(uid, VehicleVisuals.DrawDepth, out var drawDepth, args.Component))
            _sprite.SetDrawDepth(uid, drawDepth);

        // Set vehicle layer to animated or not (i.e. are the wheels turning or not)
        if (component.AutoAnimate
            && Appearance.TryGetData<bool>(uid, VehicleVisuals.AutoAnimate, out var autoAnimate, args.Component))
            _sprite.LayerSetAutoAnimated(uid, VehicleVisualLayers.AutoAnimate, autoAnimate);
    }
}

public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels
    AutoAnimate,
}
