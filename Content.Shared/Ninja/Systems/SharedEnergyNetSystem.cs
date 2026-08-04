using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Ninja.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Shared logic for the energy-netted state:
/// • Blocks input-driven movement and pull attempts.
/// • Freezes the physics body to Static so physics impulses (e.g. chain kunai) can't move the target.
/// </summary>
public sealed partial class SharedEnergyNetSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem  _blocker = default!;
    [Dependency] private PullingSystem        _pulling = default!;
    [Dependency] private SharedPhysicsSystem  _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyNettedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EnergyNettedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EnergyNettedComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<EnergyNettedComponent, PullAttemptEvent>(OnPullAttempt);
    }

    private void OnStartup(EntityUid uid, EnergyNettedComponent comp, ComponentStartup args)
    {
        // Stop any active pull
        if (TryComp<PullableComponent>(uid, out var pullable))
            _pulling.TryStopPull(uid, pullable);

        // Freeze body so physics impulses can't push the target
        if (TryComp<PhysicsComponent>(uid, out var body) && body.BodyType != BodyType.Static)
        {
            comp.OriginalBodyType = body.BodyType;
            _physics.SetBodyType(uid, BodyType.Static, body: body);
        }

        _blocker.UpdateCanMove(uid);
    }

    private void OnShutdown(EntityUid uid, EnergyNettedComponent comp, ComponentShutdown args)
    {
        // Restore original body type
        if (TryComp<PhysicsComponent>(uid, out var body) && body.BodyType == BodyType.Static)
            _physics.SetBodyType(uid, comp.OriginalBodyType, body: body);

        _blocker.UpdateCanMove(uid);
    }

    private static void OnUpdateCanMove(EntityUid uid, EnergyNettedComponent comp, UpdateCanMoveEvent args)
    {
        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private static void OnPullAttempt(EntityUid uid, EnergyNettedComponent _, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
