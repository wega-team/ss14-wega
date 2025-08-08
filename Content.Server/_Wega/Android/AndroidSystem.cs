using Content.Server.Actions;
using Content.Server.PowerCell;
using Content.Shared._Wega.Android;
using Content.Shared.Alert;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.PDA;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using JetBrains.FormatRipper.Elf;
using Robust.Server.GameObjects;
using Serilog;
using System.Linq;

namespace Content.Server._Wega.Android;

public sealed partial class AndroidSystem : SharedAndroidSystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<AndroidComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<AndroidComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<AndroidComponent, ItemToggledEvent>(OnToggled);

        SubscribeLocalEvent<AndroidComponent, LightToggleEvent>(OnLightToggle);
    }

    private void OnStartup(EntityUid uid, AndroidComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ToggleLockActionEntity, component.ToggleLockAction);
    }

    private void OnLightToggle(EntityUid uid, AndroidComponent component, LightToggleEvent args)
    {
        UpdatePointLight(uid, component);
    }

    private void UpdatePointLight(EntityUid uid, AndroidComponent component)
    {
        _pointLight.SetRadius(uid, Toggle.IsActivated(uid) ? component.BasePointLightRadiuse : component.BasePointLightRadiuse / 3f);
        _pointLight.SetEnergy(uid, Toggle.IsActivated(uid) ? component.BasePointLightEnergy : component.BasePointLightEnergy * 1.5f);

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var appearance) || !TryComp<PointLightComponent>(uid, out var light))
            return;

        if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Special, out var markings) || markings.Count == 0)
            return;

        Color ledColor = markings[0].MarkingColors[0].WithAlpha(255);
        _pointLight.SetColor(uid, ledColor, light);
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

        UpdatePointLight(uid, comp);
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
