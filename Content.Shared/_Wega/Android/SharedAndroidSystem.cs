using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Lock;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Wires;

namespace Content.Shared.Android;

public abstract partial class SharedAndroidSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<AndroidComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<AndroidComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);

        SubscribeLocalEvent<AndroidComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    #region Battery

    private void OnItemSlotInsertAttempt(EntityUid uid, AndroidComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = !ItemSlotAvailable(uid, args.User, args.Slot);
    }

    private void OnItemSlotEjectAttempt(EntityUid uid, AndroidComponent component, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = !ItemSlotAvailable(uid, args.User, args.Slot);
    }

    private bool ItemSlotAvailable(EntityUid uid, EntityUid? user, ItemSlot slot)
    {
        if (!TryComp<WiresPanelComponent>(uid, out var panel))
            return true;

        if (!panel.Open || user != null && user == uid)
            return false;

        return true;
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, AndroidComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_toggle.IsActivated(uid))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var movement))
            return;

        args.ModifySpeed(component.DischargeSpeedModifier, component.DischargeSpeedModifier);
    }

    private void OnLockToggleAttempt(EntityUid uid, AndroidComponent component, ref LockToggleAttemptEvent args)
    {
        if (args.Silent)
            return;

        args.Cancelled = true;
    }

    #endregion Battery
}
