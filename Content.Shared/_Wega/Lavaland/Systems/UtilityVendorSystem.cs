using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Lavaland.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Lavaland;

public sealed partial class UtilityVendorSystem : EntitySystem
{
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
        _itemSlots.AddItemSlot(uid, "vendor_card", component.CardSlot);
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

        return true;
    }

    private void UpdateUI(EntityUid uid, UtilityVendorComponent component)
    {
        var state = new UtilityVendorBoundUserInterfaceState(
            GetNetEntity(component.CardSlot.Item),
            component.CardSlot.Item != null ? CompOrNull<PointsCardComponent>(component.CardSlot.Item)?.Points ?? FixedPoint2.Zero : FixedPoint2.Zero,
            component.Inventory
        );
        _ui.SetUiState(uid, UtilityVendorUiKey.Key, state);
    }
}
