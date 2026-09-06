using Content.Client.Clothing;
using Content.Client.DisplacementMap;
using Content.Shared.Clothing.Components;
using Content.Shared.DirtVisuals;
using Content.Shared.DisplacementMap;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.DirtVisuals;

public sealed partial class DirtVisualsSystem : EntitySystem
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ClientClothingSystem _clothing = default!;

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
                layerIndex = _sprite.AddLayer(uid,
                    new SpriteSpecifier.Rsi(
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

    public void TryAddEquipmentDirtLayer(
        EntityUid equipee,
        EntityUid equipment,
        InventoryComponent inventory,
        SpriteComponent sprite,
        ClothingComponent clothingComponent,
        SlotDefinition slotDef,
        bool slotLayerExists,
        ref int index,
        DisplacementData? displacementData,
        HashSet<string> revealedLayers)
    {
        if (!TryComp<DirtableComponent>(equipment, out var dirtable) || !dirtable.IsDirty
           || revealedLayers.Contains($"dirt_{equipment}"))
            return;

        var dirtRsi = _cache.GetResource<RSIResource>(
            SpriteSpecifierSerializer.TextureRoot / dirtable.DirtSpritePath).RSI;

        var state = dirtable.EquippedDirtState;
        if (!string.IsNullOrEmpty(clothingComponent.EquippedPrefix))
            state = $"{clothingComponent.EquippedPrefix}-{state}";
        if (inventory.SpeciesId != null && dirtRsi.TryGetState($"{state}-{inventory.SpeciesId}", out _))
            state = $"{state}-{inventory.SpeciesId}";
        if (TryComp<ToggleableSpriteClothingComponent>(equipment, out var toggleable))
            state += toggleable.ActiveSuffix;

        if (!dirtRsi.TryGetState(state, out _))
            return;

        var dirtLayer = new PrototypeLayerData
        {
            RsiPath = dirtable.DirtSpritePath,
            State = state,
            Color = dirtable.DirtColor
        };

        var dirtKey = $"dirt_{equipment}";
        if (slotLayerExists)
        {
            index++;
            _sprite.AddBlankLayer((equipee, sprite), index);
            _sprite.LayerMapSet((equipee, sprite), dirtKey, index);
        }
        else
        {
            index = _sprite.LayerMapReserve((equipee, sprite), dirtKey);
        }

        if (sprite[index] is SpriteComponent.Layer layer)
        {
            _sprite.LayerSetData((equipee, sprite), index, dirtLayer);
            _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);
            revealedLayers.Add(dirtKey);

            if (displacementData != null)
            {
                if (_displacement.TryAddDisplacement(
                    displacementData,
                    (equipee, sprite),
                    index,
                    dirtKey,
                    out var displacementKey))
                {
                    revealedLayers.Add(displacementKey);
                    index++;
                }
            }
        }
    }
}
