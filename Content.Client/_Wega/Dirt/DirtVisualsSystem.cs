using Content.Client.Clothing;
using Content.Shared.DirtVisuals;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Client.DirtVisuals;

public sealed class DirtVisualsSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly ClientClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtableComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, DirtableComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not DirtableComponentState state)
            return;

        comp.CurrentDirtLevel = state.CurrentDirtLevel;
        comp.DirtColor = state.DirtColor;
        UpdateDirtVisuals(uid, comp);
    }

    private void UpdateDirtVisuals(EntityUid uid, DirtableComponent comp)
    {
        if (!HasComp<SpriteComponent>(uid))
            return;

        var isFolded = false;
        if (HasComp<AppearanceComponent>(uid) && _appearance.TryGetData<bool>(uid, FoldableSystem.FoldedVisuals.State, out var folded))
            isFolded = folded;

        var layerKey = $"dirt_{uid}";
        var dirtState = isFolded && !string.IsNullOrEmpty(comp.FoldingDirtState)
            ? comp.FoldingDirtState
            : comp.DirtState;

        if (comp.IsDirty)
        {
            if (!_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
            {
                layerIndex = _sprite.AddLayer(uid, new SpriteSpecifier.Rsi(
                    new ResPath(comp.DirtSpritePath),
                    dirtState
                ));
                _sprite.LayerMapSet(uid, layerKey, layerIndex);
            }

            _sprite.LayerSetVisible(uid, layerIndex, true);
            _sprite.LayerSetColor(uid, layerIndex, comp.DirtColor);

            _sprite.LayerSetRsiState(uid, layerIndex, dirtState);
        }
        else if (_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
        {
            _sprite.LayerSetVisible(uid, layerIndex, false);
        }

        if (TryComp(Transform(uid).ParentUid, out InventoryComponent? inventory))
            _clothing.InitClothing(Transform(uid).ParentUid, inventory);
    }
}
