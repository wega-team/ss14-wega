using Content.Shared.Modular.Suit;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Modular.Suit;

public sealed class AffectedModuleCarrySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AffectedModuleCarryComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<AffectedModuleCarryComponent, PullStoppedMessage>(OnPullStopped);
        SubscribeLocalEvent<AffectedModuleCarryComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<AffectedModuleCarryComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnPullStarted(Entity<AffectedModuleCarryComponent> ent, ref PullStartedMessage args)
    {
        if (args.PullerUid != ent.Owner)
            return;

        if (!_mobState.IsIncapacitated(args.PulledUid))
            return;

        ent.Comp.Active = true;
        Dirty(ent.Owner, ent.Comp);

        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnPullStopped(Entity<AffectedModuleCarryComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PullerUid != ent.Owner)
            return;

        ent.Comp.Active = false;
        Dirty(ent.Owner, ent.Comp);

        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRemove(Entity<AffectedModuleCarryComponent> ent, ref ComponentRemove args)
    {
        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRefreshSpeed(Entity<AffectedModuleCarryComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.ModifySpeed(1 + ent.Comp.Multiplier);
    }
}
