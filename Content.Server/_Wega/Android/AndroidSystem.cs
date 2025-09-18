using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Server.Stunnable;
using Content.Shared._Wega.Android;
using Content.Shared.Alert;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light;
using Content.Shared.Lock;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Standing;
using Content.Shared.Wires;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Android;

public sealed partial class AndroidSystem : SharedAndroidSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<AndroidComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<AndroidComponent, ItemToggledEvent>(OnToggled);

        SubscribeLocalEvent<AndroidComponent, LightToggleEvent>(OnLightToggle);

        SubscribeLocalEvent<AndroidComponent, ToggleLockActionEvent>(OnToggleLockAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var androidsQuery = EntityQueryEnumerator<AndroidComponent>();
        while (androidsQuery.MoveNext(out var ent, out var component))
        {
            if (!_toggle.IsActivated(ent) && _timing.CurTime > component.NextDischargeStun)
            {
                DoDischargeStun(ent, component);
                DelayDischargeStun(component);
            }
        }
    }

    private void OnStartup(EntityUid uid, AndroidComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ToggleLockActionEntity, component.ToggleLockAction);
    }

    private void OnLightToggle(EntityUid uid, AndroidComponent component, LightToggleEvent args)
    {
        UpdatePointLight(uid, component);
    }

    public void UpdatePointLight(EntityUid uid, AndroidComponent component)
    {
        _pointLight.SetRadius(uid, _toggle.IsActivated(uid) ? component.BasePointLightRadiuse : Math.Max(component.BasePointLightRadiuse / 3f, 1.3f));
        _pointLight.SetEnergy(uid, _toggle.IsActivated(uid) ? component.BasePointLightEnergy : component.BasePointLightEnergy * 1.5f);

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Special, out var markings) || markings.Count == 0)
            return;

        Color ledColor = markings[0].MarkingColors[0].WithAlpha(255);
        _pointLight.SetColor(uid, ledColor);
    }

    #region Battery

    private void OnMobStateChanged(EntityUid uid, AndroidComponent component, ref MobStateChangedEvent args)
    {
        _powerCell.SetDrawEnabled(uid, args.NewMobState == MobState.Alive);
    }

    private void OnPowerCellChanged(EntityUid uid, AndroidComponent component, PowerCellChangedEvent args)
    {
        UpdateBatteryAlert((uid, component));

        if (_powerCell.HasDrawCharge(uid))
        {
            _toggle.TryActivate(uid);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, AndroidComponent component, ref PowerCellSlotEmptyEvent args)
    {
        _toggle.TryDeactivate(uid);
    }

    private void OnToggled(EntityUid uid, AndroidComponent component, ref ItemToggledEvent args)
    {
        var drawing = _mind.TryGetMind(uid, out _, out _) && _mobState.IsAlive(uid);
        _powerCell.SetDrawEnabled(uid, drawing);

        if (!args.Activated)
        {
            component.DischargeTime = _timing.CurTime;
            DelayDischargeStun(component);
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        UpdatePointLight(uid, component);
    }

    private void UpdateBatteryAlert(Entity<AndroidComponent> ent, PowerCellSlotComponent? slotComponent = null)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, slotComponent))
        {
            _alerts.ClearAlert(ent, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent, ent.Comp.NoBatteryAlert);
            return;
        }

        var chargePercent = (short)MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        if (chargePercent == 0 && _powerCell.HasDrawCharge(ent, cell: slotComponent))
        {
            chargePercent = 1;
        }

        _alerts.ClearAlert(ent, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent, ent.Comp.BatteryAlert, chargePercent);
    }

    private void DelayDischargeStun(AndroidComponent component)
    {
        double multiplier = 1f + (_timing.CurTime - component.DischargeTime).TotalSeconds * 0.03f;

        component.NextDischargeStun = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(5f, _random.NextFloat(60f, 180f) / multiplier));
    }

    public void DoDischargeStun(EntityUid uid, AndroidComponent component)
    {
        if (TryComp<StandingStateComponent>(uid, out var standingComp) && !standingComp.Standing)
            return;

        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(5), true);

        _popup.PopupEntity(Loc.GetString("android-discharge-message"), uid, uid);
        _audio.PlayPvs(component.DischargeStunSound, uid);
    }

    private void OnToggleLockAction(EntityUid uid, AndroidComponent component, ToggleLockActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<LockComponent>(uid, out var lockComp))
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panelComp) && panelComp.Open)
        {
            _popup.PopupEntity(Loc.GetString("android-lock-panel-open"), uid, uid);
            return;
        }

        _audio.PlayPvs(!lockComp.Locked ? lockComp.LockSound : lockComp.UnlockSound, uid);
        _popup.PopupEntity(Loc.GetString(!lockComp.Locked ? "android-lock-message" : "android-unlock-message"), uid, uid);

        if (lockComp.Locked)
            _lock.Unlock(uid, uid, lockComp);
        else
            _lock.Lock(uid, uid, lockComp);

        args.Handled = true;
    }

    #endregion Battery
}
