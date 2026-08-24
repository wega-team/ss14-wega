using Content.Shared.Gatherable.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Gatherable;

public sealed partial class GatherableSystem
{
    [SubscribeLocalEvent]
    private void OnPelletCollide(Entity<PelletGatheringComponent> pellet, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        if (!TryComp<GatherableComponent>(args.OtherEntity, out var gatherable) || gatherable.Gathered)
            return;

        var pending = EnsureComp<PendingGatherComponent>(args.OtherEntity);
        pending.Hits++;
        Dirty(args.OtherEntity, pending);

        if (pending.Hits < pellet.Comp.HitsRequired)
            return;

        gatherable.Gathered = true;
        Gather((args.OtherEntity, gatherable), pellet);
    }
}
