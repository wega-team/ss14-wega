using System.Numerics;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Mobs;

public sealed class BossRewardsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BossRewardsComponent, MegafaunaKilledEvent>(OnMegafaunaKilled);
    }

    private void OnMegafaunaKilled(Entity<BossRewardsComponent> ent, ref MegafaunaKilledEvent args)
    {
        if (ent.Comp.RewardsGranted)
            return;

        GrantRewards(ent);
    }

    private void GrantRewards(Entity<BossRewardsComponent> ent)
    {
        if (ent.Comp.RewardsGranted)
            return;

        ent.Comp.RewardsGranted = true;
        var coords = Transform(ent).Coordinates;

        if (ent.Comp.GuaranteedRewards.Count > 0)
        {
            foreach (var reward in ent.Comp.GuaranteedRewards)
            {
                SpawnWithOffset(reward, coords, ent.Comp.SpawnRadius);
            }
        }

        if (ent.Comp.RandomReward.Count > 0)
        {
            var randomReward = _random.Pick(ent.Comp.RandomReward);
            SpawnWithOffset(randomReward, coords, ent.Comp.SpawnRadius);
        }

        if (ent.Comp.DeleteAfterRewards)
            QueueDel(ent);
    }

    private void SpawnWithOffset(EntProtoId prototype, EntityCoordinates center, float radius)
    {
        var offset = new Vector2(
            _random.NextFloat(-radius, radius),
            _random.NextFloat(-radius, radius)
        );

        Spawn(prototype, center.Offset(offset));
    }
}
