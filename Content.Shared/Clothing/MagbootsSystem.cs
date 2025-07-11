using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups; // Corvax-Wega-AdvMagboots
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public sealed class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Corvax-Wega-AdvMagboots

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MagbootsComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MagbootsComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<MagbootsComponent, InventoryRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);

        SubscribeLocalEvent<GravityChangedEvent>(OnGravityChanged); // Corvax-Wega-AdvMagboots
        SubscribeLocalEvent<MagbootsUserComponent, EntParentChangedMessage>(OnMagbootsParentChanged); // Corvax-Wega-AdvMagboots
    }

    private void OnToggled(Entity<MagbootsComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;
        // only stick to the floor if being worn in the correct slot
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, comp.Slot, out var worn)
            && uid == worn)
        {
            UpdateMagbootEffects(container.Owner, ent, args.Activated);
        }
    }

    private void OnGotUnequipped(Entity<MagbootsComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, false);
    }

    private void OnGotEquipped(Entity<MagbootsComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateMagbootEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    public void UpdateMagbootEffects(EntityUid user, Entity<MagbootsComponent> ent, bool state)
    {
        // TODO: public api for this and add access
        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !state;

        if (state)
            _alerts.ShowAlert(user, ent.Comp.MagbootsAlert);
        else
            _alerts.ClearAlert(user, ent.Comp.MagbootsAlert);
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !_toggle.IsActivated(ent.Owner))
            return;

        // do not cancel weightlessness if the person is in off-grid.
        if (ent.Comp.RequiresGrid && !_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnIsWeightless(Entity<MagbootsComponent> ent, ref InventoryRelayedEvent<IsWeightlessEvent> args)
    {
        OnIsWeightless(ent, ref args.Args);
    }

    // Corvax-Wega-AdvMagboots-start
    private void OnGravityChanged(ref GravityChangedEvent args)
    {
        var query = EntityQueryEnumerator<MagbootsComponent, ItemToggleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var magboots, out _, out var xform))
        {
            if (xform.GridUid != args.ChangedGridIndex && xform.MapUid != args.ChangedGridIndex)
                continue;

            if (!_container.TryGetContainingContainer((uid, null, null), out var container))
                continue;

            if (!_inventory.TryGetSlotEntity(container.Owner, magboots.Slot, out var worn) || uid != worn)
                continue;

            var shouldBeActive = !args.HasGravity;
            if (_toggle.IsActivated(uid) != shouldBeActive)
            {
                if (!shouldBeActive && magboots.DisabledAutoOff)
                    return;

                _toggle.Toggle(uid, container.Owner);

                if (shouldBeActive)
                    _popup.PopupClient(Loc.GetString("magboots-auto-on"), container.Owner, container.Owner);
                else
                    _popup.PopupClient(Loc.GetString("magboots-auto-off"), container.Owner, container.Owner);
            }
        }
    }

    private void OnMagbootsParentChanged(Entity<MagbootsUserComponent> ent, ref EntParentChangedMessage args)
    {
        if (!_inventory.TryGetSlotEntity(ent, "shoes", out var worn) || !TryComp<MagbootsComponent>(worn, out var magboots))
            return;

        if (args.Transform.GridUid == null)
            return;

        var hasGravity = _gravity.EntityGridOrMapHaveGravity((args.Transform.GridUid.Value, null));

        var shouldBeActive = !hasGravity;
        if (_toggle.IsActivated(worn.Value) != shouldBeActive)
        {
            if (!shouldBeActive && magboots.DisabledAutoOff)
                return;

            _toggle.Toggle(worn.Value, ent);

            if (shouldBeActive)
                _popup.PopupClient(Loc.GetString("magboots-auto-on"), ent, ent);
            else
                _popup.PopupClient(Loc.GetString("magboots-auto-off"), ent, ent);
        }
    }

    public bool IsWearingMagboots(EntityUid uid)
    {
        return _inventory.TryGetSlotEntity(uid, "shoes", out var boots)
            && HasComp<MagbootsComponent>(boots);
    }

    public bool IsMagbootsActive(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "shoes", out var boots))
            return false;

        return _toggle.IsActivated(boots.Value);
    }

    public void ToggleMagboots(EntityUid uid, EntityUid? user = null)
    {
        if (!_inventory.TryGetSlotEntity(uid, "shoes", out var boots)
            || !HasComp<MagbootsComponent>(boots))
            return;

        _toggle.Toggle(boots.Value, user ?? uid);
    }
    // Corvax-Wega-AdvMagboots-end
}
