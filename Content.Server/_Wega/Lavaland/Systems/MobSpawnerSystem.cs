using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Lavaland.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class MobSpawnerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobSpawnerComponent, MapInitEvent>(OnSpawnerMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<MobSpawnerComponent>();

        while (query.MoveNext(out var uid, out var spawner))
        {
            if (!spawner.IsActive && HasPlayersInRange(uid, spawner.ActivationRadius))
            {
                spawner.IsActive = true;
                spawner.NextSpawnTime = curTime + spawner.SpawnInterval;
            }

            foreach (var mob in spawner.SpawnedMobs)
            {
                if (_mobState.IsIncapacitated(mob) || !Exists(mob))
                    spawner.SpawnedMobs.Remove(mob);
            }

            if (!spawner.IsActive || spawner.SpawnedMobs.Count >= spawner.MaxSpawns)
                continue;

            if (spawner.NextSpawnTime > curTime)
                continue;

            TrySpawnMob(uid, spawner, curTime);
        }
    }

    private void OnSpawnerMapInit(Entity<MobSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawnTime = _timing.CurTime + ent.Comp.SpawnInterval;
    }

    private void TrySpawnMob(EntityUid uid, MobSpawnerComponent spawner, TimeSpan curTime)
    {
        var newMob = SpawnMonster(uid, spawner);
        if (newMob != null) spawner.SpawnedMobs.Add(newMob.Value);

        spawner.NextSpawnTime = curTime + spawner.SpawnInterval;
    }

    private EntityUid? SpawnMonster(EntityUid uid, MobSpawnerComponent component)
    {
        if (component.SpawnWeights.Count == 0)
            return null;

        var monsterProto = GetWeightedRandom(component.SpawnWeights);

        var coordinates = Transform(uid).Coordinates;
        var spawnPos = coordinates.Offset(_random.NextVector2(component.SpawnRadius));

        return SpawnAtPosition(monsterProto, spawnPos);
    }

    private EntProtoId GetWeightedRandom(Dictionary<EntProtoId, float> weights)
    {
        var current = 0f;
        var totalWeight = weights.Values.Sum();
        var randomValue = _random.NextFloat(0, totalWeight);

        foreach (var (prototype, weight) in weights)
        {
            current += weight;
            if (randomValue <= current)
                return prototype;
        }

        return weights.Keys.First();
    }

    private bool HasPlayersInRange(EntityUid uid, float radius)
    {
        var coordinates = Transform(uid).Coordinates;
        var query = _lookup.GetEntitiesInRange<ActorComponent>(coordinates, radius);
        foreach (var (actorUid, _) in query)
        {
            if (HasComp<GhostComponent>(actorUid) || TryComp(actorUid, out LavalandVisitorComponent? visitor) && visitor.ImmuneToStorm)
                continue;

            return true;
        }

        return false;
    }
}
