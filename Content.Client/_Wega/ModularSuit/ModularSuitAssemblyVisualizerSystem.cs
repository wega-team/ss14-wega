using Content.Shared.Modular.Suit;
using Robust.Client.GameObjects;

namespace Content.Client.Modular.Suit;

/// <summary>
/// Handles the sprite state changes while
/// constructing mech assemblies.
/// </summary>
public sealed class ModularSuitAssemblyVisualizerSystem : VisualizerSystem<ModularSuitAssemblyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ModularSuitAssemblyVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData<int>(uid, ModilarSuitAssemblyVisuals.State, out var stage, args.Component))
            return;

        var state = component.StatePrefix + stage;
        SpriteSystem.LayerSetRsiState(uid, 0, state);
    }
}
