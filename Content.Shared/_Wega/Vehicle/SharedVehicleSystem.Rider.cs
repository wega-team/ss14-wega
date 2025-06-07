using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle;

public abstract partial class SharedVehicleSystem
{
    private void InitializeRider()
    {
        SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnPullAttempt);
    }

    private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity == component.Vehicle)
        {
            _buckle.TryUnbuckle(uid, null);
        }
    }

    private void OnPullAttempt(EntityUid uid, RiderComponent component, PullAttemptEvent args)
    {
        if (component.Vehicle != null)
            args.Cancelled = true;
    }
}