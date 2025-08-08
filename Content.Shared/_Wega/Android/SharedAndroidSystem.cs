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
    [Dependency] protected readonly SharedPointLightSystem PointLight = default!;

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

    private void OnLockToggleAttempt(Entity<AndroidComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (args.Silent)
            return;

        args.Cancelled = true;
    }

    #endregion Battery

    public void UpdatePointLight(EntityUid uid, AndroidComponent component)
    {
        PointLight.SetRadius(uid, Toggle.IsActivated(uid) ? component.BasePointLightRadiuse : Math.Max(component.BasePointLightRadiuse / 3f, 1.3f));
        PointLight.SetEnergy(uid, Toggle.IsActivated(uid) ? component.BasePointLightEnergy : component.BasePointLightEnergy * 1.5f);

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Special, out var markings) || markings.Count == 0)
            return;

        Color ledColor = markings[0].MarkingColors[0].WithAlpha(255);
        PointLight.SetColor(uid, ledColor);
    }

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
