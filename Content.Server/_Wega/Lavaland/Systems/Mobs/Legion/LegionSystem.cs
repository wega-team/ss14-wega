using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Mobs;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Lavaland.Mobs;

public sealed partial class LegionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly EntProtoId Reward = "";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegionBossComponent, MapInitEvent>(OnLegionMapInit);
        SubscribeLocalEvent<LegionBossComponent, MegafaunaAttackEvent>(OnLegionAttack);
        SubscribeLocalEvent<LegionBossComponent, MegafaunaKilledEvent>(OnLegionKilled);

        SubscribeLocalEvent<LegionSplitComponent, MegafaunaKilledEvent>(OnSplitKilled);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateLegionState(frameTime);
    }

    private void OnLegionMapInit(EntityUid uid, LegionBossComponent component, MapInitEvent args)
    {
        component.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(component.StateSwitchInterval);
        component.NextSummonTime = _timing.CurTime;
        component.NextChargeTime = _timing.CurTime;
    }

    private void OnLegionAttack(EntityUid uid, LegionBossComponent component, ref MegafaunaAttackEvent args)
    {
        switch (component.CurrentState)
        {
            case LegionState.Summoning:
                UpdateSummoningState(uid, component);
                break;
            case LegionState.Charging:
                UpdateChargingState(uid, component);
                break;
        }
    }

    #region State Management

    private void UpdateLegionState(float frameTime)
    {
        var query = EntityQueryEnumerator<LegionBossComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (_timing.CurTime >= component.NextStateSwitchTime)
            {
                SwitchState(component);
                component.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(component.StateSwitchInterval);
            }
        }
    }

    private void SwitchState(LegionBossComponent component)
    {
        component.CurrentState = component.CurrentState == LegionState.Summoning
            ? LegionState.Charging
            : LegionState.Summoning;
    }

    #endregion

    #region Summoning State

    private void UpdateSummoningState(EntityUid uid, LegionBossComponent component)
    {
        if (_timing.CurTime < component.NextSummonTime)
            return;

        SummonMinions(uid, component);
        component.NextSummonTime = _timing.CurTime + TimeSpan.FromSeconds(component.SummonInterval);
    }

    private void SummonMinions(EntityUid uid, LegionBossComponent component)
    {
        if (_megafauna.FindAttackTarget(uid) == null)
            return;

        var selfCoords = Transform(uid).Coordinates;
        for (int i = 0; i < component.SummonCount; i++)
        {
            var spawnPos = FindSpawnPositionNear(selfCoords, 3f);
            if (spawnPos != null)
            {
                Spawn(component.MinionPrototype, spawnPos.Value);
            }
        }
    }

    #endregion

    #region Charging State

    private void UpdateChargingState(EntityUid uid, LegionBossComponent component)
    {
        if (_timing.CurTime < component.NextChargeTime)
            return;

        ChargeAtTarget(uid, component);
        component.NextChargeTime = _timing.CurTime + TimeSpan.FromSeconds(component.ChargeInterval);
    }

    private void ChargeAtTarget(EntityUid uid, LegionBossComponent component)
    {
        var target = _megafauna.FindAttackTarget(uid);
        if (target == null)
            return;

        var xform = Transform(uid);
        var targetCoords = Transform(target.Value).Coordinates;

        var direction = (targetCoords.Position - xform.Coordinates.Position).Normalized();
        var throwing = direction * 6f;
        var throwTarget = xform.Coordinates.Offset(throwing);

        _throwing.TryThrow(uid, throwTarget, 15f);
    }

    #endregion

    #region Splitting System

    private void OnLegionKilled(EntityUid uid, LegionBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        SpawnLootWithChance(component, coords);

        if (HasComp<LegionSplitComponent>(uid)) // Skip when is splited
            return;

        foreach (var prototype in component.SplitPrototypes)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos != null)
            {
                Spawn(prototype, spawnPos.Value);
            }
        }

        QueueDel(uid);
    }

    private void OnSplitKilled(EntityUid uid, LegionSplitComponent component, MegafaunaKilledEvent args)
    {
        if (!string.IsNullOrEmpty(component.NextSplitPrototype))
        {
            SplitToNextLevel(uid, component);
        }
        else
        {
            var allSplits = EntityQuery<LegionSplitComponent>().ToList();
            if (allSplits.Count != 1)
                return;

            Spawn(Reward, Transform(uid).Coordinates);
        }

        QueueDel(uid);
    }

    private void SplitToNextLevel(EntityUid uid, LegionSplitComponent component)
    {
        var coords = Transform(uid).Coordinates;
        for (int i = 0; i < 2; i++)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos != null)
            {
                Spawn(component.NextSplitPrototype, spawnPos.Value);
            }
        }
    }

    private void SpawnLootWithChance(LegionBossComponent component, EntityCoordinates coords)
    {
        foreach (var (prototype, chance) in component.LootPrototypes)
        {
            if (_random.Prob(chance))
            {
                var spawnPos = FindSpawnPositionNear(coords, 1.5f);
                if (spawnPos != null)
                {
                    Spawn(prototype, spawnPos.Value);
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    private EntityCoordinates? FindSpawnPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 5; i++)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var distance = _random.NextFloat(1f, maxDistance);
            var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;

            var testCoords = center.Offset(offset);

            if (CanMoveTo(testCoords))
                return testCoords;
        }
        return null;
    }

    private bool CanMoveTo(EntityCoordinates coords)
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
