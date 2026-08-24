using Content.Server.Destructible;
using Content.Server.Projectiles;
using Content.Shared.Gatherable;
using Content.Shared.Gatherable.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Gatherable;

public sealed partial class PiercingGatherProjectileSystem : EntitySystem
{
    [Dependency] private DestructibleSystem _destructible = default!;

    [SubscribeLocalEvent]
    private void OnPiercedPreventCollide(Entity<PiercingGatherComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Pierced.Contains(args.OtherEntity))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent(after: [typeof(ProjectileSystem), typeof(GatherableSystem)])]
    private void OnPiercedCollide(Entity<PiercingGatherComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture)
            return;

        RemComp<TargetedProjectileComponent>(ent.Owner);

        if (ent.Comp.Depth <= 0)
        {
            QueueDel(ent.Owner);
            return;
        }

        if (!ent.Comp.Pierced.Add(args.OtherEntity))
        {
            KeepFlying(ent.Owner);
            return;
        }

        var brokenRock = HasComp<MiningScannerViewableComponent>(args.OtherEntity)
                         && (TerminatingOrDeleted(args.OtherEntity) || EntityManager.IsQueuedForDeletion(args.OtherEntity));

        if (!brokenRock)
        {
            QueueDel(ent.Owner);
            return;
        }

        if (_destructible.DestroyedAt(args.OtherEntity) > ent.Comp.MaxDurability)
        {
            QueueDel(ent.Owner);
            return;
        }

        ent.Comp.Depth--;

        if (ent.Comp.Depth <= 0)
        {
            QueueDel(ent.Owner);
            return;
        }

        KeepFlying(ent.Owner);
    }

    private void KeepFlying(EntityUid bolt)
    {
        if (!TryComp<ProjectileComponent>(bolt, out var projectile))
            return;

        projectile.ProjectileSpent = false;
    }
}
