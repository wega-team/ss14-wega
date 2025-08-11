using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Lock;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Android;

public abstract partial class SharedAndroidSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<AndroidComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
        SubscribeLocalEvent<AndroidComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);

        SubscribeLocalEvent<AndroidComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    #region Battery

    private void OnItemSlotInsertAttempt(EntityUid uid, AndroidComponent component, ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = !ItemSlotAvailable(uid, args.User, args.Slot);
    }

    private void OnItemSlotEjectAttempt(EntityUid uid, AndroidComponent component, ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = !ItemSlotAvailable(uid, args.User, args.Slot);
    }

    private bool ItemSlotAvailable(EntityUid uid, EntityUid? user, ItemSlot slot)
    {
        if (!TryComp<WiresPanelComponent>(uid, out var panel))
            return true;

        if (!panel.Open)
            return false;

        if (user != null && user == uid)
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

    private void OnLockToggleAttempt(EntityUid uid, AndroidComponent component, LockToggleAttemptEvent args)
    {
        if (args.Silent)
            return;

        args.Cancelled = true;
    }

    #endregion Battery
}
