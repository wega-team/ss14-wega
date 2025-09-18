using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Components;
using Content.Shared.Destructible;
using Content.Shared.Ghost;
using Content.Shared.Lavaland.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed class NecropolisTendrilSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NecropolisTendrilComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NecropolisTendrilComponent, DestructionEventArgs>(OnDestruction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<NecropolisTendrilComponent>();

        while (query.MoveNext(out var uid, out var tendril))
        {
            if (!tendril.IsActive && HasPlayersInRange(uid, tendril.ActivationRadius))
            {
                tendril.IsActive = true;
                tendril.NextSpawnTime = curTime + tendril.SpawnInterval;
            }

            if (!tendril.IsActive || tendril.SpawnedCount >= tendril.MaxSpawns)
                continue;

            if (tendril.NextSpawnTime > curTime)
                continue;

            SpawnMonster(uid, tendril);

            tendril.NextSpawnTime = curTime + tendril.SpawnInterval;
            tendril.SpawnedCount++;
        }
    }

    private void OnMapInit(Entity<NecropolisTendrilComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.IsActive = false;
        ent.Comp.SpawnedCount = 0;
    }

    private void OnDestruction(Entity<NecropolisTendrilComponent> ent, ref DestructionEventArgs args)
    {
        var coordinates = Transform(ent).Coordinates;
        var chasmPrototype = ent.Comp.ChasmPrototype;

        Timer.Spawn(TimeSpan.FromSeconds(ent.Comp.ChasmDelay), () =>
        {
            CreateChasms(coordinates, chasmPrototype);
        });
    }

    private void SpawnMonster(EntityUid uid, NecropolisTendrilComponent component)
    {
        if (component.SpawnWeights.Count == 0)
            return;

        var monsterProto = GetWeightedRandom(component.SpawnWeights);

        var coordinates = Transform(uid).Coordinates;
        var spawnPos = coordinates.Offset(_random.NextVector2(component.SpawnRadius));

        SpawnAtPosition(monsterProto, spawnPos);
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
            if (HasComp<GhostComponent>(actorUid) || TryComp(actorUid, out LavalandVisitorComponent? visitor)
                && visitor.ImmuneToStorm)
                continue;

            return true;
        }

        return false;
    }

    private void CreateChasms(EntityCoordinates coordinates, EntProtoId chasmProto)
    {
        if (!coordinates.IsValid(EntityManager))
            return;

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var offset = new Vector2(x, y);
                var chasmPos = coordinates.Offset(offset);

                if (chasmPos.IsValid(EntityManager))
                    SpawnAtPosition(chasmProto, chasmPos);
            }
        }

        var extraChasms = _random.Next(3, 8);
        var expansionDirections = new List<Vector2>
        {
            new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, -2), new Vector2(2, 2),
            new Vector2(-2, -1), new Vector2(-2, 0), new Vector2(-2, 1),
            new Vector2(2, -1), new Vector2(2, 0), new Vector2(2, 1),
            new Vector2(-1, -2), new Vector2(0, -2), new Vector2(1, -2),
            new Vector2(-1, 2), new Vector2(0, 2), new Vector2(1, 2)
        };

        _random.Shuffle(expansionDirections);

        for (var i = 0; i < extraChasms && i < expansionDirections.Count; i++)
        {
            var chasmPos = coordinates.Offset(expansionDirections[i]);
            if (chasmPos.IsValid(EntityManager))
                SpawnAtPosition(chasmProto, chasmPos);
        }
    }
}
