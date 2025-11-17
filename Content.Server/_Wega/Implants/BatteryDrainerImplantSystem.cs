using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Power.Components;
using Robust.Server.Audio;

namespace Content.Server._Wega.Implants;

public sealed class BatteryDrainerImplantSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerImplantComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BatteryDrainerImplantComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BatteryDrainerImplantComponent, BatteryChargeActionEvent>(OnChargeAction);
        SubscribeLocalEvent<BatteryDrainerImplantComponent, BatteryDischargeActionEvent>(OnDischargeAction);
    }

    private void OnInit(EntityUid uid, BatteryDrainerImplantComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ChargeActionEntity, component.ChargeAction);
        _actions.AddAction(uid, ref component.DischargeActionEntity, component.DischargeAction);
    }

    private void OnShutdown(EntityUid uid, BatteryDrainerImplantComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ChargeActionEntity);
        _actions.RemoveAction(uid, component.DischargeActionEntity);
    }

    private void OnChargeAction(EntityUid uid, BatteryDrainerImplantComponent component, BatteryChargeActionEvent args)
    {
        args.Handled = true;
        _actions.StartUseDelay(component.DischargeActionEntity);

        var battery = TryGetBattery(uid);
        if (battery == null)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-target"), uid, uid);
            return;
        }

        ChargeBattery(uid, component, battery.Value);
    }

    private void OnDischargeAction(EntityUid uid, BatteryDrainerImplantComponent component, BatteryDischargeActionEvent args)
    {
        args.Handled = true;
        _actions.StartUseDelay(component.ChargeActionEntity);

        var battery = TryGetBattery(uid);
        if (battery == null)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-target"), uid, uid);
            return;
        }

        DischargeBattery(uid, component, battery.Value);
    }

    private EntityUid? TryGetBattery(EntityUid uid)
    {
        EntityUid? battery = null;
        foreach (var entity in _hands.EnumerateHeld(uid))
        {
            if (HasComp<BatteryComponent>(entity))
            {
                battery = entity;
                break;
            }
        }

        return battery;
    }

    private void ChargeBattery(EntityUid user, BatteryDrainerImplantComponent component, EntityUid target)
    {
        if (!_powerCell.TryGetBatteryFromSlot(user, out var battery))
            return;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
            return;

        float transfer = Math.Clamp(targetBattery.MaxCharge - targetBattery.CurrentCharge, 0f, battery.CurrentCharge);
        if (transfer == 0f)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-transfer"), user, user);
            return;
        }
        _powerCell.TryUseCharge(user, transfer);
        _battery.ChangeCharge(target, transfer);

        _audio.PlayPvs(component.UseSound, user);
    }

    private void DischargeBattery(EntityUid user, BatteryDrainerImplantComponent component, EntityUid target)
    {
        if (!_powerCell.TryGetBatteryFromSlot(user, out var batteryUid, out var battery))
            return;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
            return;

        float transfer = Math.Clamp(battery.MaxCharge - battery.CurrentCharge, 0f, targetBattery.CurrentCharge);
        if (transfer == 0f)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-transfer"), user, user);
            return;
        }
        _battery.ChangeCharge(batteryUid.Value, transfer);
        _battery.UseCharge(target, transfer);

        _audio.PlayPvs(component.UseSound, user);
    }
}
