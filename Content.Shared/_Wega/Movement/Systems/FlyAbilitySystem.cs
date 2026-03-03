using Robust.Shared.Timing;
using Content.Shared.Damage.Systems;
using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Movement.Systems;

public abstract class SharedFlyAbilitySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;


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
                if (!TryComp<NoSlipComponent>(ent, out var _))
                    EnsureComp<NoSlipComponent>(ent);
                if (!TryComp<MovementAlwaysTouchingComponent>(ent, out var _))
                    EnsureComp<MovementAlwaysTouchingComponent>(ent);
                if (!TryComp<CanMoveInAirComponent>(ent, out var _))
                    EnsureComp<CanMoveInAirComponent>(ent);
                if (ent.Comp.Sound != null && !TryComp<AmbientSoundComponent>(ent, out var _))
                {
                    EnsureComp<AmbientSoundComponent>(ent);
                    _ambient.SetSound(ent.Owner, ent.Comp.Sound);
                    _ambient.SetRange(ent.Owner, ent.Comp.SoundRange);
                    _ambient.SetVolume(ent.Owner, ent.Comp.SoundVolume);
                }
            }
        }
        else
        {
            if (TryComp<PhysicsComponent>(ent, out var physics))
            {
                _physics.SetBodyStatus(ent, physics, BodyStatus.OnGround, true);
                if (TryComp<NoSlipComponent>(ent, out var _))
                    RemComp<NoSlipComponent>(ent);
                if (TryComp<MovementAlwaysTouchingComponent>(ent, out var _))
                    RemComp<MovementAlwaysTouchingComponent>(ent);
                if (TryComp<CanMoveInAirComponent>(ent, out var _))
                    RemComp<CanMoveInAirComponent>(ent);
                if (TryComp<AmbientSoundComponent>(ent, out var _))
                    RemComp<AmbientSoundComponent>(ent);
            }
        }
    }
}
