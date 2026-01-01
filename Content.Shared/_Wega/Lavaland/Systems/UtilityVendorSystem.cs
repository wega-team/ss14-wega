using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Lavaland.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Lavaland;

public sealed partial class UtilityVendorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UtilityVendorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UtilityVendorComponent, BoundUIOpenedEvent>(UpdateUiState);
        SubscribeLocalEvent<UtilityVendorComponent, EntInsertedIntoContainerMessage>(UpdateUiState);
        SubscribeLocalEvent<UtilityVendorComponent, EntRemovedFromContainerMessage>(UpdateUiState);
        SubscribeLocalEvent<UtilityVendorComponent, UtilityVendorPurchaseMessage>(OnPurchaseMessage);
    }

    private void OnComponentInit(EntityUid uid, UtilityVendorComponent component, ComponentInit args)
    {
        if (!_itemSlots.TryGetSlot(uid, "vendor_card", out var slot))
            return;

        slot.Whitelist = new EntityWhitelist
        {
            Components = new[] { "PointsCard" }
        };

        component.CardSlot = slot;
    }

    private void UpdateUiState<T>(EntityUid uid, UtilityVendorComponent component, ref T ev)
    {
        UpdateUI(uid, component);
    }

    private void OnPurchaseMessage(EntityUid uid, UtilityVendorComponent component, UtilityVendorPurchaseMessage args)
    {
        TryPurchaseItem(uid, args.ItemId, component);
    }

    public bool TryPurchaseItem(EntityUid uid, string itemId, UtilityVendorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.CardSlot.Item is not { } card || !TryComp<PointsCardComponent>(card, out var pointsCard))
            return false;

        if (!component.Inventory.TryGetValue(itemId, out var price) || pointsCard.Points < price)
            return false;

        pointsCard.Points -= price;
        Dirty(card, pointsCard);

        UpdateUI(uid, component);

        Spawn(itemId, Transform(uid).Coordinates);
        _audio.PlayPvs(component.SoundVend, uid);

        return true;
    }

    private void UpdateUI(EntityUid uid, UtilityVendorComponent component)
    {
        var state = new UtilityVendorBoundUserInterfaceState(
            component.CardSlot.Item != null ? CompOrNull<PointsCardComponent>(component.CardSlot.Item)?.Points ?? FixedPoint2.Zero : FixedPoint2.Zero,
            component.Inventory
        );
        _ui.SetUiState(uid, UtilityVendorUiKey.Key, state);
    }
}
