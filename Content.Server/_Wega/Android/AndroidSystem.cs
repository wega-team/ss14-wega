using Content.Server.Actions;
using Content.Server.PowerCell;
using Content.Shared._Wega.Android;
using Content.Shared.Alert;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using JetBrains.FormatRipper.Elf;

namespace Content.Server._Wega.Android;

public sealed partial class AndroidSystem : SharedAndroidSystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<AndroidComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<AndroidComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnStartup(Entity<AndroidComponent> uid, AndroidComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid.Owner, uid.Comp.ToggleLockAction);
    }

    private void OnMobStateChanged(EntityUid uid, AndroidComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            if (_mind.TryGetMind(uid, out _, out _))
                _powerCell.SetDrawEnabled(uid, true);
        }
        else
        {
            _powerCell.SetDrawEnabled(uid, false);
        }
    }

    private void OnPowerCellChanged(EntityUid uid, AndroidComponent component, PowerCellChangedEvent args)
    {
        UpdateBatteryAlert((uid, component));

        if (_powerCell.HasDrawCharge(uid))
        {
            Toggle.TryActivate(uid);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, AndroidComponent component, ref PowerCellSlotEmptyEvent args)
    {
        Toggle.TryDeactivate(uid);
    }

    private void OnToggled(Entity<AndroidComponent> ent, ref ItemToggledEvent args)
    {
        var (uid, comp) = ent;

        var drawing = _mind.TryGetMind(uid, out _, out _) && _mobState.IsAlive(ent);
        _powerCell.SetDrawEnabled(uid, drawing);

        if (!args.Activated)
        {
            comp.DischargeTime = Timing.CurTime;
            DelayDischargeStun(comp);
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
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
}
