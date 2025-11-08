using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Systems;
using Content.Shared.Xenobiology.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._Wega.Xenobiology;

public sealed class CellVisualsSystem : SharedCellVisualsSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellContainerVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<CellContainerVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<CellContainerComponent>(ent, out var containerComponent) || containerComponent.Empty)
            return;

        Color? color = null;
        foreach (var cell in containerComponent.Cells)
        {
            color ??= cell.Color;
            color = Color.InterpolateBetween(color.Value, cell.Color, 0.5f);
        }

        if (color is null)
            return;

        _sprite.LayerSetColor(ent.Owner, CellContainerVisuals.DishLayer, color.Value);
    }
}
