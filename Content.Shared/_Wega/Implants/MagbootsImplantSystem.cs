using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Toggleable;

namespace Content.Shared._Wega.Implants;

public sealed class SharedMagbootsImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsImplantComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MagbootsImplantComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MagbootsImplantComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<MagbootsImplantComponent, IsWeightlessEvent>(OnIsWeightless);
    }

    private void OnInit(EntityUid uid, MagbootsImplantComponent component, ComponentInit args)
    {
        var action = _actions.AddAction(uid, component.ToggleAction);
        component.ToggleActionEntity = action;
    }

    private void OnShutdown(EntityUid uid, MagbootsImplantComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ToggleActionEntity);
    }

    private void OnToggleAction(EntityUid uid, MagbootsImplantComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Action != component.ToggleActionEntity)
            return;

        component.Toggled = !args.Action.Comp.Toggled;
        UpdateMagbootEffects(uid, component);
        Dirty(uid, component);

        args.Toggle = true;
        args.Handled = true;
    }

    public void UpdateMagbootEffects(EntityUid user, MagbootsImplantComponent component)
    {
        if (TryComp<MovedByPressureComponent>(user, out var moved))
            moved.Enabled = !component.Toggled;

        _gravity.RefreshWeightless(user);

        if (component.Toggled)
            _alerts.ShowAlert(user, component.MagbootsAlert);
        else
            _alerts.ClearAlert(user, component.MagbootsAlert);
    }

    private void OnIsWeightless(EntityUid uid, MagbootsImplantComponent component, ref IsWeightlessEvent args)
    {
        if (args.Handled || !component.Toggled)
            return;

        if (!_gravity.EntityOnGravitySupportingGridOrMap(uid))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }
}
