using Content.Shared._Wega.Seraphim;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Wega.Seraphim;

public sealed partial class SeraphimMarkVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private TagSystem _tag = default!;

    private const string OuterClothingSlot = "outerClothing";
    private const string NeckSlot = "neck";
    private const string SpriteState = "body";
    private static readonly ResPath SpritePath = new("/Textures/_Wega/Mobs/Customization/seraphim.rsi");
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";

    private enum SeraphimMarkVisualLayers : byte
    {
        Body,
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeraphimMarkVisualComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SeraphimMarkVisualComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SeraphimMarkVisualComponent, DidEquipEvent>(OnEquipped);
        SubscribeLocalEvent<SeraphimMarkVisualComponent, DidUnequipEvent>(OnUnequipped);
    }

    private void OnStartup(Entity<SeraphimMarkVisualComponent> ent, ref ComponentStartup args)
    {
        RefreshLayer(ent);
    }

    private void OnShutdown(Entity<SeraphimMarkVisualComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite)
            && _sprite.LayerMapTryGet((ent.Owner, sprite), SeraphimMarkVisualLayers.Body, out _, false))
        {
            _sprite.RemoveLayer((ent.Owner, sprite), SeraphimMarkVisualLayers.Body);
        }
    }

    private void OnEquipped(Entity<SeraphimMarkVisualComponent> ent, ref DidEquipEvent args)
    {
        if (args.Slot != OuterClothingSlot && args.Slot != NeckSlot)
            return;

        RefreshLayer(ent);
    }

    private void OnUnequipped(Entity<SeraphimMarkVisualComponent> ent, ref DidUnequipEvent args)
    {
        if (args.Slot != OuterClothingSlot && args.Slot != NeckSlot)
            return;

        RefreshLayer(ent);
    }

    // Слой под закладкой outerClothing
    private void RefreshLayer(Entity<SeraphimMarkVisualComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent, ref sprite, false))
            return;

        var hasMark = _sprite.LayerMapTryGet((ent.Owner, sprite), SeraphimMarkVisualLayers.Body, out var markIndex, false);
        var hasOuter = _sprite.LayerMapTryGet((ent.Owner, sprite), OuterClothingSlot, out var outerIndex, false);

        // Слой на месте
        if (hasMark && (!hasOuter || markIndex < outerIndex))
        {
            UpdateVisibility((ent.Owner, sprite));
            return;
        }

        if (hasMark)
            _sprite.RemoveLayer((ent.Owner, sprite), SeraphimMarkVisualLayers.Body, false);

        // Ниже слоёв одежды
        int? index = hasOuter ? outerIndex : null;

        var layer = _sprite.AddLayer(
            (ent.Owner, sprite),
            new SpriteSpecifier.Rsi(SpritePath, SpriteState),
            index);

        _sprite.LayerMapSet((ent.Owner, sprite), SeraphimMarkVisualLayers.Body, layer);
        sprite.LayerSetShader(layer, "unshaded");

        UpdateVisibility((ent.Owner, sprite));
    }

    // Скрытие под скафандром
    private void UpdateVisibility(Entity<SpriteComponent?> ent)
    {
        var hidden = _inventory.TryGetSlotEntity(ent.Owner, OuterClothingSlot, out var outer)
                     && outer is { } outerUid
                     && _tag.HasTag(outerUid, HardsuitTag);

        _sprite.LayerSetVisible(ent, SeraphimMarkVisualLayers.Body, !hidden);
    }
}
