using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class TheThingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BossMusicSystem _bossMusic = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TheThingBossComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<TheThingBossComponent, TheThingChargeActionEvent>(OnCharge);
        SubscribeLocalEvent<TheThingBossComponent, TheThingDecimateActionEvent>(OnDecimate);
        SubscribeLocalEvent<TheThingBossComponent, TheThingShriekActionEvent>(OnShriek);
        SubscribeLocalEvent<TheThingBossComponent, TheThingSquareTendrilsActionEvent>(OnSquareTendrils);
        SubscribeLocalEvent<TheThingBossComponent, TheThingCardinalTendrilsActionEvent>(OnCardinalTendrils);
        SubscribeLocalEvent<TheThingBossComponent, TheThingAcidShowerActionEvent>(OnAcidShower);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnMobStateChanged(Entity<TheThingBossComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        if (ent.Comp.NextStage == null)
            return;

        TransformToNextStage(ent, ent.Comp.NextStage.Value);
    }

    private void TransformToNextStage(Entity<TheThingBossComponent> ent, EntProtoId protoId)
    {
        var coords = Transform(ent).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var damageContrib = Comp<MegafaunaDamageContributorComponent>(ent);

        var nextStage = Spawn(protoId, coords);
        _bossMusic.TransferBossMusic(ent.Owner, nextStage);

        var nextContrib = EnsureComp<MegafaunaDamageContributorComponent>(nextStage);
        nextContrib.TotalDamageReceived = damageContrib.TotalDamageReceived;
        nextContrib.Contributors = damageContrib.Contributors;

        QueueDel(ent);
    }

    private void OnCharge(Entity<TheThingBossComponent> ent, ref TheThingChargeActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var target = args.Target;
        var xform = Transform(ent);
        var targetCoords = Transform(target).Coordinates;

        var direction = (targetCoords.Position - xform.Coordinates.Position).Normalized();
        var throwTarget = xform.Coordinates.Offset(direction * args.ChargeDistance);

        _throwing.TryThrow(ent, throwTarget, args.ChargeForce);
    }

    private void OnDecimate(Entity<TheThingBossComponent> ent, ref TheThingDecimateActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var coords = Transform(ent).Coordinates;
        SpawnAreaSpikes(coords, args.AreaSize, args.SpikePrototype);
    }

    private void OnShriek(Entity<TheThingBossComponent> ent, ref TheThingShriekActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var coords = Transform(ent).Coordinates;
        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(coords, args.AreaSize / 2f, LookupFlags.Uncontained);

        foreach (var entity in entities)
        {
            if (entity.Owner == ent.Owner)
                continue;

            if (_random.Prob(args.StunChance))
            {
                _stun.TryAddParalyzeDuration(entity.Owner, TimeSpan.FromSeconds(args.StunDuration));
            }
        }
    }

    private void OnSquareTendrils(Entity<TheThingBossComponent> ent, ref TheThingSquareTendrilsActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var target = args.Target;
        var areaSize = args.AreaSize;
        var spikeProto = args.SpikePrototype;

        var targetCoords = Transform(target).Coordinates;
        var warning = Spawn(args.WarningPrototype, targetCoords);

        Timer.Spawn(TimeSpan.FromSeconds(args.WarningDelay), () =>
        {
            if (!Exists(ent) || _mobState.IsDead(ent))
                return;

            if (!Exists(target) || _mobState.IsDead(target))
                return;

            SpawnAreaSpikes(targetCoords, areaSize, spikeProto);
        });
    }

    private void OnCardinalTendrils(Entity<TheThingBossComponent> ent, ref TheThingCardinalTendrilsActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var coords = Transform(ent).Coordinates;
        var directions = new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) };

        foreach (var dir in directions)
        {
            for (int i = 1; i <= args.Length; i++)
            {
                var offset = dir * i;
                var spawnCoords = coords.Offset(offset);
                if (CanSpawnAt(spawnCoords))
                    Spawn(args.SpikePrototype, spawnCoords);
            }
        }
    }

    private void OnAcidShower(Entity<TheThingBossComponent> ent, ref TheThingAcidShowerActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var targetCoords = Transform(args.Target).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var pattern = GenerateRandomAcidPattern(4);

        foreach (var offset in pattern)
        {
            var spawnCoords = targetCoords.Offset(offset);
            if (CanSpawnAt(spawnCoords))
                Spawn(args.AcidPrototype, spawnCoords);
        }
    }

    #region Utility Methods

    private void SpawnAreaSpikes(EntityCoordinates center, int size, EntProtoId spikePrototype)
    {
        int half = size / 2;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                var offset = new Vector2(x, y);
                var spawnCoords = center.Offset(offset);
                if (CanSpawnAt(spawnCoords))
                    Spawn(spikePrototype, spawnCoords);
            }
        }
    }

    private List<Vector2> GenerateRandomAcidPattern(int size)
    {
        var pattern = new List<Vector2>();
        var half = size / 2;

        pattern.Add(new Vector2(0, 0));

        var totalCells = size * size - 1;
        var targetCount = (int)Math.Ceiling(totalCells * _random.NextFloat(0.5f, 0.7f));

        var availablePositions = new List<Vector2>();
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                availablePositions.Add(new Vector2(x, y));
            }
        }

        availablePositions = availablePositions.OrderBy(_ => _random.Next()).ToList();
        var selected = availablePositions.Take(targetCount).ToList();

        pattern.AddRange(selected);

        if (_random.Prob(0.3f))
        {
            var extra = availablePositions.Except(selected).ToList();
            if (extra.Count > 0)
            {
                var extraCount = _random.Next(1, Math.Min(3, extra.Count));
                pattern.AddRange(extra.Take(extraCount));
            }
        }

        return pattern;
    }

    private bool CanSpawnAt(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coords);
        if (!_map.TryGetTileRef(gridUid.Value, grid, tilePos, out var tileRef))
            return false;

        return !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable);
    }

    #endregion
}
