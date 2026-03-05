using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Actions.Components;
using Content.Server.Damage.Systems;
using Content.Server.Popups;

namespace Content.Server.Movement.Systems;

public sealed class FlyAbilitySystem : SharedFlyAbilitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    private Entity<ActionComponent> _action;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlyAbilityComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<FlyAbilityComponent, FlyAbilityEvent>(SwitchFly);
        SubscribeLocalEvent<FlyAbilityComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FlyAbilityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<StaminaComponent>(uid, out var stamina)
                || !comp.Active
                || Timing.CurTime < comp.NextTickTime)
                continue;

            comp.NextTickTime = Timing.CurTime + TimeSpan.FromSeconds(comp.Interval);

            _stamina.TryTakeStamina(uid, comp.StaminaDamage);
            if (stamina.StaminaDamage > stamina.CritThreshold * 0.65f)
                DeactivateFly(uid, comp, _action);
        }
    }

    private void OnCompInit(Entity<FlyAbilityComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
    }

    private void SwitchFly(Entity<FlyAbilityComponent> ent, ref FlyAbilityEvent args)
    {
        _action = args.Action;

        if (!ent.Comp.Active)
        {
            ActivateFly(ent, _action);
        }
        else
        {
            DeactivateFly(ent.Owner, ent.Comp, _action);
        }
    }

    private void ActivateFly(Entity<FlyAbilityComponent> ent, Entity<ActionComponent> action)
    {
        _popup.PopupEntity(Loc.GetString("fly-activated-massage"), ent.Owner, ent.Owner);

        ent.Comp.SprintSpeedCurrent += ent.Comp.SprintSpeedModifier;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        ent.Comp.Active = !ent.Comp.Active;

        var ev = new SwitchFlyAbility(action, ent.Comp.Active);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void DeactivateFly(EntityUid uid, FlyAbilityComponent component, Entity<ActionComponent> action)
    {
        _popup.PopupEntity(Loc.GetString("fly-deactivated-massage"), uid, uid);

        component.SprintSpeedCurrent = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        _actions.SetCooldown(action.Owner, component.CooldownDelay);


        component.Active = !component.Active;

        var ev = new SwitchFlyAbility(action, component.Active);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnRefreshMovespeed(Entity<FlyAbilityComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SprintSpeedCurrent, ent.Comp.SprintSpeedCurrent);
    }
}
