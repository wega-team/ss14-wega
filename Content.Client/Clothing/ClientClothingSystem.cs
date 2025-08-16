using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client.DisplacementMap;
using Content.Client.Inventory;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DirtVisuals; // Corvax-Wega-Dirtable
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates; // Corvax-Wega-ToggleClothing
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Clothing;

public sealed class ClientClothingSystem : ClothingSystem
{
    public const string Jumpsuit = "jumpsuit";

    /// <summary>
    /// This is a shitty hotfix written by me (Paul) to save me from renaming all files.
    /// For some context, im currently refactoring inventory. Part of that is slots not being indexed by a massive enum anymore, but by strings.
    /// Problem here: Every rsi-state is using the old enum-names in their state. I already used the new inventoryslots ALOT. tldr: its this or another week of renaming files.
    /// </summary>
    private static readonly Dictionary<string, string> TemporarySlotMap = new()
    {
        {"head", "HELMET"},
        {"eyes", "EYES"},
        {"ears", "EARS"},
        {"mask", "MASK"},
        {"outerClothing", "OUTERCLOTHING"},
        {Jumpsuit, "INNERCLOTHING"},
        {"neck", "NECK"},
        {"back", "BACKPACK"},
        {"belt", "BELT"},
        {"gloves", "HAND"},
        {"shoes", "FEET"},
        /// Corvax-Wega-start
	    {"socks", "SOCKS"},
        {"underweartop", "UNDERWEARTOP"},
        {"underwearbottom", "UNDERWEARBOTTOM"},
        {"anal", "ANAL"},
        /// Corvax-Wega-end
        {"id", "IDCARD"},
        {"pocket1", "POCKET1"},
        {"pocket2", "POCKET2"},
        {"suitstorage", "SUITSTORAGE"},
    };

    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GetEquipmentVisualsEvent>(OnGetVisuals);
        SubscribeLocalEvent<InventoryComponent, InventoryTemplateUpdated>(OnInventoryTemplateUpdated);

        SubscribeLocalEvent<ToggleableSpriteClothingComponent, ComponentHandleState>(OnHandleState); // Corvax-Wega-ToggleClothing

        SubscribeLocalEvent<InventoryComponent, VisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<InventoryComponent, AppearanceChangeEvent>(OnAppearanceUpdate);
    }

    private void OnAppearanceUpdate(EntityUid uid, InventoryComponent component, ref AppearanceChangeEvent args)
    {
        // May need to update displacement maps if the sex changed. Also required to properly set the stencil on init
        if (args.Sprite == null)
            return;

        UpdateAllSlots(uid, component);

        // No clothing equipped -> make sure the layer is hidden, though this should already be handled by on-unequip.
        if (_sprite.LayerMapTryGet((uid, args.Sprite), HumanoidVisualLayers.StencilMask, out var layer, false))
        {
            DebugTools.Assert(!args.Sprite[layer].Visible);
            _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
        }
    }

    private void OnInventoryTemplateUpdated(Entity<InventoryComponent> ent, ref InventoryTemplateUpdated args)
    {
        UpdateAllSlots(ent.Owner, ent.Comp);
    }

    private void UpdateAllSlots(
        EntityUid uid,
        InventoryComponent? inventoryComponent = null)
    {
        var enumerator = _inventorySystem.GetSlotEnumerator((uid, inventoryComponent));
        while (enumerator.NextItem(out var item, out var slot))
        {
            RenderEquipment(uid, item, slot.Name, inventoryComponent);
        }
    }

    private void OnGetVisuals(EntityUid uid, ClothingComponent item, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;

        List<PrototypeLayerData>? layers = null;
        // Corvax-Wega-ToggleClothing-start
        var suffix = TryComp<ToggleableSpriteClothingComponent>(uid, out var toggleable)
            ? toggleable.ActiveSuffix
            : string.Empty;
        // Corvax-Wega-ToggleClothing-end

        // first attempt to get species specific data.
        if (inventory.SpeciesId != null)
            item.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // if that returned nothing, attempt to find generic data
        if (layers == null && !item.ClothingVisuals.TryGetValue(args.Slot, out layers))
        {
            // No generic data either. Attempt to generate defaults from the item's RSI & item-prefixes
            if (!TryGetDefaultVisuals(uid, item, args.Slot, inventory.SpeciesId, out layers))
                return;
        }

        // add each layer to the visuals
        var i = 0;
        foreach (var layer in layers)
        {
            // Corvax-Wega-ToggleClothing-Edit-start
            var originalState = layer.State;
            if (string.IsNullOrEmpty(originalState))
                continue;

            var newState = originalState;
            if (!string.IsNullOrEmpty(suffix))
            {

                var suffixedState = $"{originalState}{suffix}";
                if (StateExists(uid, suffixedState, inventory.SpeciesId))
                    newState = suffixedState;
                else if (!originalState.StartsWith("equipped-"))
                    continue;
            }

            var key = layer.MapKeys?.FirstOrDefault() ?? $"{args.Slot}-{i++}";
            args.Layers.Add((key, new PrototypeLayerData
            {
                MapKeys = layer.MapKeys,
                RsiPath = layer.RsiPath,
                State = newState,
                Color = layer.Color,
                Scale = layer.Scale,
                Shader = layer.Shader
            }));
            // Corvax-Wega-ToggleClothing-Edit-end
        }
    }

    /// <summary>
    ///     If no explicit clothing visuals were specified, this attempts to populate with default values.
    /// </summary>
    /// <remarks>
    ///     Useful for lazily adding clothing sprites without modifying yaml. And for backwards compatibility.
    /// </remarks>
    private bool TryGetDefaultVisuals(EntityUid uid, ClothingComponent clothing, string slot, string? speciesId,
        [NotNullWhen(true)] out List<PrototypeLayerData>? layers)
    {
        layers = null;

        RSI? rsi = null;

        if (clothing.RsiPath != null)
            rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / clothing.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        if (rsi == null)
            return false;

        var correctedSlot = slot;
        TemporarySlotMap.TryGetValue(correctedSlot, out correctedSlot);

        // Corvax-Wega-ToggleClothing-Edit-start
        var suffix = string.Empty;
        if (TryComp<ToggleableSpriteClothingComponent>(uid, out var toggleable))
        {
            suffix = toggleable.ActiveSuffix;
        }

        var state = $"equipped-{correctedSlot}{suffix}";

        if (!string.IsNullOrEmpty(clothing.EquippedPrefix))
            state = $"{clothing.EquippedPrefix}-equipped-{correctedSlot}{suffix}";

        if (clothing.EquippedState != null)
            state = $"{clothing.EquippedState}{suffix}";
        // Corvax-Wega-ToggleClothing-Edit-end

        // species specific
        if (speciesId != null && rsi.TryGetState($"{state}-{speciesId}", out _))
            state = $"{state}-{speciesId}";
        else if (!rsi.TryGetState(state, out _))
            return false;

        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;
        layers = new() { layer };

        return true;
    }

    private void OnVisualsChanged(EntityUid uid, InventoryComponent component, VisualsChangedEvent args)
    {
        var item = GetEntity(args.Item);

        if (!TryComp(item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, item, clothing.InSlot, component, null, clothing);
    }

    private void OnDidUnequip(Entity<SpriteComponent> entity, ref DidUnequipEvent args)
    {
        if (!TryComp(entity, out InventorySlotsComponent? inventorySlots))
            return;

        if (!inventorySlots.VisualLayerKeys.TryGetValue(args.Slot, out var revealedLayers))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        foreach (var layer in revealedLayers)
        {
            _sprite.RemoveLayer(entity.AsNullable(), layer);
        }
        revealedLayers.Clear();
    }

    public void InitClothing(EntityUid uid, InventoryComponent component)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var enumerator = _inventorySystem.GetSlotEnumerator((uid, component));
        while (enumerator.NextItem(out var item, out var slot))
        {
            RenderEquipment(uid, item, slot.Name, component, sprite);
        }
    }

    protected override void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        RenderEquipment(args.Equipee, uid, args.Slot, clothingComponent: component);
    }

    private void RenderEquipment(EntityUid equipee, EntityUid equipment, string slot,
        InventoryComponent? inventory = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null,
        InventorySlotsComponent? inventorySlots = null)
    {
        if (!Resolve(equipee, ref inventory, ref sprite, ref inventorySlots) ||
           !Resolve(equipment, ref clothingComponent, false))
        {
            return;
        }

        if (!_inventorySystem.TryGetSlot(equipee, slot, out var slotDef, inventory))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        if (inventorySlots.VisualLayerKeys.TryGetValue(slot, out var revealedLayers))
        {
            foreach (var key in revealedLayers)
            {
                _sprite.RemoveLayer((equipee, sprite), key);
            }
            revealedLayers.Clear();
        }
        else
        {
            revealedLayers = new();
            inventorySlots.VisualLayerKeys[slot] = revealedLayers;
        }

        var ev = new GetEquipmentVisualsEvent(equipee, slot);
        RaiseLocalEvent(equipment, ev);

        if (ev.Layers.Count == 0)
        {
            RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
            return;
        }

        // temporary, until layer draw depths get added. Basically: a layer with the key "slot" is being used as a
        // bookmark to determine where in the list of layers we should insert the clothing layers.
        var slotLayerExists = _sprite.LayerMapTryGet((equipee, sprite), slot, out var index, false);

        // Select displacement maps
        var displacementData = inventory.Displacements.GetValueOrDefault(slot); //Default unsexed map

        var equipeeSex = CompOrNull<HumanoidAppearanceComponent>(equipee)?.Sex;
        if (equipeeSex != null)
        {
            switch (equipeeSex)
            {
                case Sex.Male:
                    if (inventory.MaleDisplacements.Count > 0)
                        displacementData = inventory.MaleDisplacements.GetValueOrDefault(slot);
                    break;
                case Sex.Female:
                    if (inventory.FemaleDisplacements.Count > 0)
                        displacementData = inventory.FemaleDisplacements.GetValueOrDefault(slot);
                    break;
            }
        }

        // add the new layers
        foreach (var (key, layerData) in ev.Layers)
        {
            if (!revealedLayers.Add(key))
            {
                Log.Warning($"Duplicate key for clothing visuals: {key}. Are multiple components attempting to modify the same layer? Equipment: {ToPrettyString(equipment)}");
                continue;
            }

            if (slotLayerExists)
            {
                index++;
                // note that every insertion requires reshuffling & remapping all the existing layers.
                _sprite.AddBlankLayer((equipee, sprite), index);
                _sprite.LayerMapSet((equipee, sprite), key, index);

                if (layerData.Color != null)
                    _sprite.LayerSetColor((equipee, sprite), key, layerData.Color.Value);
                if (layerData.Scale != null)
                    _sprite.LayerSetScale((equipee, sprite), key, layerData.Scale.Value);
            }
            else
                index = _sprite.LayerMapReserve((equipee, sprite), key);

            if (sprite[index] is not Layer layer)
                continue;

            // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
            if (layerData.RsiPath == null
                && layerData.TexturePath == null
                && layer.RSI == null
                && TryComp(equipment, out SpriteComponent? clothingSprite))
            {
                _sprite.LayerSetRsi(layer, clothingSprite.BaseRSI);
            }

            _sprite.LayerSetData((equipee, sprite), index, layerData);
            _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);

            if (displacementData is not null)
            {
                //Checking that the state is not tied to the current race. In this case we don't need to use the displacement maps.
                if (layerData.State is not null && inventory.SpeciesId is not null && layerData.State.EndsWith(inventory.SpeciesId))
                    continue;

                if (_displacement.TryAddDisplacement(displacementData, (equipee, sprite), index, key, out var displacementKey))
                {
                    revealedLayers.Add(displacementKey);
                    index++;
                }
            }
        }

        // Corvax-Wega-Dirtable-start
        if (TryComp<DirtableComponent>(equipment, out var dirtable) &&
            dirtable.IsDirty &&
            !revealedLayers.Contains($"dirt_{equipment}"))
        {
            RSI? dirtRsi = null;
            if (dirtable.DirtSpritePath != null)
            {
                dirtRsi = _cache.GetResource<RSIResource>(
                    SpriteSpecifierSerializer.TextureRoot / dirtable.DirtSpritePath).RSI;
            }

            if (dirtRsi != null)
            {
                var state = dirtable.EquippedDirtState;
                if (!string.IsNullOrEmpty(clothingComponent.EquippedPrefix))
                    state = $"{clothingComponent.EquippedPrefix}-{state}";
                if (inventory.SpeciesId != null && dirtRsi.TryGetState($"{state}-{inventory.SpeciesId}", out _))
                    state = $"{state}-{inventory.SpeciesId}";
                if (TryComp<ToggleableSpriteClothingComponent>(equipment, out var toggleable))
                    state += toggleable.ActiveSuffix;

                if (dirtRsi.TryGetState(state, out _))
                {
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

                    // Accounting for a displacements
                    if (sprite[index] is Layer layer)
                    {
                        _sprite.LayerSetData((equipee, sprite), index, dirtLayer);
                        _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);
                        revealedLayers.Add(dirtKey);

                        if (displacementData is not null)
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
        }
        // Corvax-Wega-Dirtable-end

        RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
    }

    // Corvax-Wega-ToggleClothing-start
    private bool StateExists(EntityUid uid, string state, string? speciesId)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) && sprite.BaseRSI != null)
        {
            if (!string.IsNullOrEmpty(speciesId))
            {
                var speciesState = $"{state}-{speciesId}";
                if (sprite.BaseRSI.TryGetState(speciesState, out _))
                    return true;
            }

            return sprite.BaseRSI.TryGetState(state, out _);
        }
        return false;
    }

    private void OnHandleState(EntityUid uid, ToggleableSpriteClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ToggleableSpriteClothingComponentState state)
            return;

        component.ActiveSuffix = state.ActiveSuffix;
        UpdateClothingVisuals(uid);
    }

    private void UpdateClothingVisuals(EntityUid uid)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || clothing.InSlot == null)
            return;

        var parent = Transform(uid).ParentUid;
        if (!HasComp<SpriteComponent>(parent) || !TryComp<InventoryComponent>(parent, out var inventory))
            return;

        RenderEquipment(parent, uid, clothing.InSlot, inventory, clothingComponent: clothing);
    }
    // Corvax-Wega-ToggleClothing-end
}
