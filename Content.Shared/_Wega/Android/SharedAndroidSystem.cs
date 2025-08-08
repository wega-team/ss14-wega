using Content.Shared._Wega.Resomi;
using Content.Shared._Wega.Resomi.Abilities.Hearing;
using Content.Shared.Access.Systems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Crawling;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Lock;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Sound;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Android;

public abstract partial class SharedAndroidSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;

    [Dependency] protected readonly ItemToggleSystem Toggle = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedStunSystem Stun = default!;
    [Dependency] protected readonly SharedCrawlingSystem Crawling = default!;
    [Dependency] protected readonly LockSystem Lock = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
        SubscribeLocalEvent<AndroidComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);

        SubscribeLocalEvent<AndroidComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);

        SubscribeLocalEvent<AndroidComponent, ToggleLockActionEvent>(OnToggleLockAction);
        SubscribeLocalEvent<AndroidComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var androidsQuery = EntityQueryEnumerator<AndroidComponent>();
        while (androidsQuery.MoveNext(out var ent, out var component))
        {
            if (!Toggle.IsActivated(ent) && Timing.CurTime > component.NextDischargeStun)
            {
                DoDischargeStun(ent, component);
                DelayDischargeStun(component);
            }
        }
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

        if (!panel.Open)
            return false;

        if (user != null && user == uid)
        {
            return false;
        }

        return true;
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, AndroidComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (Toggle.IsActivated(uid))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var movement))
            return;

        args.ModifySpeed(component.DischargeSpeedModifier, component.DischargeSpeedModifier);
    }

    private void OnToggleLockAction(Entity<AndroidComponent> ent, ref ToggleLockActionEvent args)
    {
        if (args.Handled)
            return;

        var (uid, comp) = ent;

        if (!TryComp<LockComponent>(uid, out var lockComp))
            return;

        Audio.PlayPvs(!lockComp.Locked ? lockComp.LockSound : lockComp.UnlockSound, uid, new AudioParams());
        Popup.PopupEntity(Loc.GetString(!lockComp.Locked ? "android-lock-message" : "android-unlock-message"), uid, uid);

        if (lockComp.Locked)
            Lock.Unlock(uid, uid, lockComp);
        else
            Lock.Lock(uid, uid, lockComp);

        args.Handled = true;
    }

    private void OnLockToggleAttempt(Entity<AndroidComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (args.Silent)
            return;

        args.Cancelled = true;
    }

    #endregion Battery

    public void DelayDischargeStun(AndroidComponent component)
    {
        double multiplier = 1f + (Timing.CurTime - component.DischargeTime).TotalSeconds * 0.03f;

        component.NextDischargeStun = Timing.CurTime + TimeSpan.FromSeconds(Math.Max(5f, Random.NextFloat(60f, 180f) / multiplier));
    }

    public void DoDischargeStun(EntityUid uid, AndroidComponent component)
    {
        if (TryComp<CrawlingComponent>(uid, out var crawlingComp) && crawlingComp.IsCrawling)
            return;

        Stun.TryParalyze(uid, TimeSpan.FromSeconds(5), true);

        Popup.PopupEntity(Loc.GetString("android-discharge-message"), uid, uid);
        Audio.PlayPvs(component.DischargeStunSound, uid, new AudioParams());
    }
}
