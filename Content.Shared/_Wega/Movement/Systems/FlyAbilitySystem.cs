using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedFlyAbilitySystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlyAbilityComponent, SwitchFlyAbility>(OnSwitchFlyAbility);
    }

    private void OnSwitchFlyAbility(Entity<FlyAbilityComponent> ent, ref SwitchFlyAbility args)
    {
        OnHandleStateChange(ent, ref args);
        ToggleComponent(ent, ref args);
    }

    private void OnHandleStateChange(Entity<FlyAbilityComponent> ent, ref SwitchFlyAbility args)
    {
        _actions.SetToggled(args.Action.Owner, args.Toggled);
    }

    private void ToggleComponent(Entity<FlyAbilityComponent> ent, ref SwitchFlyAbility args)
    {
        if (args.Toggled)
        {
            if (TryComp<PhysicsComponent>(ent, out var physics))
            {
                _physics.SetBodyStatus(ent, physics, BodyStatus.InAir, true);
                if (!HasComp<NoSlipComponent>(ent))
                    EnsureComp<NoSlipComponent>(ent);

                if (!HasComp<MovementAlwaysTouchingComponent>(ent))
                    EnsureComp<MovementAlwaysTouchingComponent>(ent);

                if (!HasComp<CanMoveInAirComponent>(ent))
                    EnsureComp<CanMoveInAirComponent>(ent);


                if (ent.Comp.Sound != null && !HasComp<FootstepModifierComponent>(ent))
                {
                    var footstepModifierComp = EnsureComp<FootstepModifierComponent>(ent);
                    footstepModifierComp.FootstepSoundCollection = ent.Comp.Sound;
                }
            }
        }
        else
        {
            if (TryComp<PhysicsComponent>(ent, out var physics))
            {
                _physics.SetBodyStatus(ent, physics, BodyStatus.OnGround, true);
                if (HasComp<NoSlipComponent>(ent))
                    RemComp<NoSlipComponent>(ent);

                if (HasComp<MovementAlwaysTouchingComponent>(ent))
                    RemComp<MovementAlwaysTouchingComponent>(ent);

                if (HasComp<CanMoveInAirComponent>(ent))
                    RemComp<CanMoveInAirComponent>(ent);

                if (HasComp<FootstepModifierComponent>(ent))
                    RemComp<FootstepModifierComponent>(ent);
            }
        }
    }
}
