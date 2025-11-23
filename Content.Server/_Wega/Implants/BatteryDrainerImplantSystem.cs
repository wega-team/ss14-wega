using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Power.Components;
using NetCord;
using Robust.Server.Audio;
using static Content.Server.Power.Pow3r.PowerState;

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

        var target = FindBatteryInHands(uid);
        if (target == null)
            return;

        TransferCharge(component, uid, target.Value, uid);
    }

    private void OnDischargeAction(EntityUid uid, BatteryDrainerImplantComponent component, BatteryDischargeActionEvent args)
    {
        args.Handled = true;
        _actions.StartUseDelay(component.ChargeActionEntity);

        var source = FindBatteryInHands(uid);
        if (source == null)
            return;

        TransferCharge(component, source.Value, uid, uid);
    }

    private EntityUid? FindBatteryInHands(EntityUid uid)
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

        if (battery == null)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-target"), uid, uid);
            return null;
        }

        return battery;
    }

    private EntityUid? TryGetBattery(EntityUid uid)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery, out _))
        {
            if (TryComp<BatteryComponent>(uid, out var comp))
                return uid;
        }
        else
            return battery;

        return null;
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

    private void TransferCharge(BatteryDrainerImplantComponent component, EntityUid source, EntityUid target, EntityUid user)
    {
        var sourceUid = TryGetBattery(source);
        var targetUid = TryGetBattery(target);

        if (sourceUid == null || targetUid == null)
            return;

        if (!TryComp<BatteryComponent>(sourceUid, out var sourceBattery) || !TryComp<BatteryComponent>(targetUid, out var targetBattery))
            return;

        float transfer = Math.Clamp(targetBattery.MaxCharge - targetBattery.CurrentCharge, 0f, sourceBattery.CurrentCharge);
        if (transfer == 0f)
        {
            _popup.PopupEntity(Loc.GetString("implant-battery-drainer-no-transfer"), user, user);
            return;
        }

        _battery.ChangeCharge(targetUid.Value, transfer);
        _battery.UseCharge(sourceUid.Value, transfer);

        _audio.PlayPvs(component.UseSound, source);
    }
}
