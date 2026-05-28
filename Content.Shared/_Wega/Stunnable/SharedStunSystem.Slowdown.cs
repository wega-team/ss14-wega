using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem
{
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    private void OnSlowdownOnContactCollide(Entity<SlowdownOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(args.OtherEntity, MovementModStatusSystem.Slowdown, ent.Comp.Duration, ent.Comp.Multiplier);
    }
}
