using System.Linq;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client.Visuals;

/// <summary>
/// A visual system is necessary to optimize the process of creating and displaying a visual, without creating a of different visual systems.
/// Only for integration via code.
/// </summary>
public sealed class VisualStateSystem : VisualizerSystem<VisualStateComponent>
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VisualStateComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<VisualStateComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
    }

    protected override void OnAppearanceChange(EntityUid uid, VisualStateComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, VisualLayers.Enabled, out var enabled, args.Component))
            return;

        UpdateVisualState(uid, component, enabled, args.Sprite);
    }

    public void UpdateVisualState(EntityUid uid, VisualStateComponent component, bool enabled, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        if (component.MainLayer != null &&
            SpriteSystem.LayerMapTryGet((uid, sprite), component.MainLayer, out var layerIndex, false))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), layerIndex, enabled);
        }

        _item.VisualsChanged(uid);
    }

    private void OnGetEquipmentVisuals(EntityUid uid, VisualStateComponent component, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance) ||
            !AppearanceSystem.TryGetData<bool>(uid, VisualLayers.Enabled, out var enabled, appearance) ||
            !enabled)
            return;

        if (!component.EquipmentStates.TryGetValue(args.Slot, out var layers))
            return;

        var modulateColor = AppearanceSystem.TryGetData<Color>(uid, VisualLayers.Color, out var color, appearance);

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault() ?? $"{args.Slot}-visual-{i}";

            if (modulateColor)
                layer.Color = color;

            args.Layers.Add((key, layer));
            i++;
        }
    }

    private void OnGetHeldVisuals(EntityUid uid, VisualStateComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance) ||
            !AppearanceSystem.TryGetData<bool>(uid, VisualLayers.Enabled, out var enabled, appearance) ||
            !enabled)
            return;

        if (!component.InhandStates.TryGetValue(args.Location.ToString(), out var layers))
            return;

        var modulateColor = AppearanceSystem.TryGetData<Color>(uid, VisualLayers.Color, out var color, appearance);

        var i = 0;
        var baseKey = $"inhand-{args.Location}";
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault() ?? $"{baseKey}-visual-{i}";

            if (modulateColor)
                layer.Color = color;

            args.Layers.Add((key, layer));
            i++;
        }
    }
}
