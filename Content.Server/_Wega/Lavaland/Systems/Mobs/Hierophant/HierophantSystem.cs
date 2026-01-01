using System.Linq;
using System.Numerics;
using Content.Shared.Damage;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Lavaland.Mobs;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class HierophantSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float LowHealthThreshold = 0.5f;
    private static readonly EntProtoId SpawnPrototype = "HierophantLavalandSquare";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantBossComponent, MapInitEvent>(OnHierophantMapInit);
        SubscribeLocalEvent<HierophantBossComponent, DamageChangedEvent>(OnHierophantDamage);
        SubscribeLocalEvent<HierophantBossComponent, MegafaunaAttackEvent>(OnHierophantAttack);
        SubscribeLocalEvent<HierophantBossComponent, MegafaunaKilledEvent>(OnHierophantKilled);

        SubscribeLocalEvent<HierophantChaserComponent, ComponentStartup>(OnChaserStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateChasers(frameTime);
        UpdatePassiveMovement(frameTime);
        UpdateReturnToBase(frameTime);
    }

    #region Passive Movement

    private void OnHierophantMapInit(EntityUid uid, HierophantBossComponent component, MapInitEvent args)
    {
        component.HomePosition = Transform(uid).Coordinates;
        component.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.PassiveMoveInterval);
        component.NextReturnCheckTime = _timing.CurTime + TimeSpan.FromMinutes(component.ReturnCheckInterval);
    }

    private void UpdatePassiveMovement(float frameTime)
    {
        var query = EntityQueryEnumerator<HierophantBossComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextPassiveMoveTime)
                continue;

            MoveTowardsNearestTarget(uid);
            component.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.PassiveMoveInterval);
        }
    }

    private void UpdateReturnToBase(float frameTime)
    {
        var query = EntityQueryEnumerator<HierophantBossComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextReturnCheckTime)
                continue;

            CheckReturnToBase(uid, component);
            component.NextReturnCheckTime = _timing.CurTime + TimeSpan.FromMinutes(component.ReturnCheckInterval);
        }
    }

    private void CheckReturnToBase(EntityUid uid, HierophantBossComponent component)
    {
        if (_megafauna.FindAttackTarget(uid) != null || !component.NeedComeBack)
            return;

        ReturnToBase(uid, component);
    }

    private void ReturnToBase(EntityUid uid, HierophantBossComponent component)
    {
        var currentPos = Transform(uid).Coordinates;

        Spawn3x3Area(currentPos);

        _transform.SetCoordinates(uid, component.HomePosition);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), Transform(uid).Coordinates);
        component.NeedComeBack = false;

        Spawn3x3Area(component.HomePosition);
    }

    private void MoveTowardsNearestTarget(EntityUid uid)
    {
        var target = _megafauna.FindAttackTarget(uid);
        if (target == null)
            return;

        var selfCoords = Transform(uid).Coordinates;
        var targetCoords = Transform(target.Value).Coordinates;

        var direction = (targetCoords.Position - selfCoords.Position).Normalized();
        var newCoords = selfCoords.Offset(direction);

        var correctedCoords = GetTileCenter(newCoords);

        if (CanSpawnAt(correctedCoords))
        {
            _transform.SetCoordinates(uid, correctedCoords);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), Transform(uid).Coordinates);
        }
    }

    private EntityCoordinates GetTileCenter(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return coords;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return coords;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coords);
        return _map.GridTileToLocal(gridUid.Value, grid, tilePos);
    }

    #endregion

    #region Attack System

    private void OnHierophantDamage(EntityUid uid, HierophantBossComponent component, DamageChangedEvent args)
    {
        if (args.DamageIncreased && TryComp<MegafaunaAttacksComponent>(uid, out var attacks))
        {
            var healthRatio = GetHealthRatio(uid);
            var speedMultiplier = GetAttackSpeedMultiplier(healthRatio);
            attacks.BaseAttackCooldown = Math.Max(1.5f, 3f / speedMultiplier);
            if (!component.NeedComeBack)
                component.NeedComeBack = true;
        }
    }

    private void OnHierophantAttack(EntityUid uid, HierophantBossComponent component, ref MegafaunaAttackEvent args)
    {
        var isLowHealth = IsLowHealth(uid);
        var attackType = SelectAttackType(component);

        PerformAttack(uid, component, attackType, args.Target, isLowHealth);
        component.LastAttack = attackType;
    }

    private HierophantAttackType SelectAttackType(HierophantBossComponent component)
    {
        var allAttacks = new[]
        {
            HierophantAttackType.Blink,
            HierophantAttackType.Crosses,
            HierophantAttackType.Chasers,
            HierophantAttackType.DamageArea
        };

        var availableAttacks = allAttacks.Where(a => a != component.LastAttack).ToList();
        return availableAttacks.Count == 0 ? _random.Pick(allAttacks) : _random.Pick(availableAttacks);
    }

    private void PerformAttack(EntityUid uid, HierophantBossComponent component,
        HierophantAttackType attackType, EntityUid target, bool isLowHealth)
    {
        switch (attackType)
        {
            case HierophantAttackType.Blink:
                PerformBlinkAttack(uid, component, target, isLowHealth);
                break;
            case HierophantAttackType.Crosses:
                PerformCrossAttack(uid, component, target, isLowHealth);
                break;
            case HierophantAttackType.Chasers:
                PerformChaserAttack(uid, component, target, isLowHealth);
                break;
            case HierophantAttackType.DamageArea:
                PerformDamageAreaAttack(uid, component, isLowHealth);
                break;
        }
    }

    #endregion

    #region Attack Implementations

    private void PerformBlinkAttack(EntityUid uid, HierophantBossComponent component, EntityUid target, bool isLowHealth)
    {
        var selfCoords = Transform(uid).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        Spawn3x3Area(selfCoords);

        var blinkPos = FindBlinkPosition(targetCoords);
        if (blinkPos != null)
        {
            _transform.SetCoordinates(uid, blinkPos.Value);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), Transform(uid).Coordinates);
            Spawn3x3Area(blinkPos.Value);
        }

        if (isLowHealth)
        {
            Timer.Spawn(500, () =>
            {
                if (Deleted(uid)) return;

                var newBlinkPos = FindBlinkPosition(targetCoords);
                if (newBlinkPos != null)
                {
                    _transform.SetCoordinates(uid, newBlinkPos.Value);
                    _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), Transform(uid).Coordinates);
                    Spawn3x3Area(newBlinkPos.Value);
                }
            });
        }
    }

    private void PerformCrossAttack(EntityUid uid, HierophantBossComponent component, EntityUid target, bool isLowHealth)
    {
        var lineLength = 8;
        var targetCoords = Transform(target).Coordinates;

        if (isLowHealth)
        {
            SpawnCrossLines(targetCoords, lineLength, false);
            SpawnCrossLines(targetCoords, lineLength, true);
        }
        else
        {
            var useDiagonal = _random.Next(2) == 0;
            SpawnCrossLines(targetCoords, lineLength, useDiagonal);
        }
    }

    private void PerformChaserAttack(EntityUid uid, HierophantBossComponent component, EntityUid target, bool isLowHealth)
    {
        var selfCoords = Transform(uid).Coordinates;

        var baseCount = 1;
        var extraCount = isLowHealth ? 1 : 0;
        var chasersToSpawn = baseCount + extraCount;

        var spawnDelay = 0f;
        for (int i = 0; i < chasersToSpawn; i++)
        {
            var currentDelay = spawnDelay;
            Timer.Spawn((int)(currentDelay * 1000), () =>
            {
                if (Deleted(uid)) return;

                var chaserPos = FindSpawnPositionNear(selfCoords, 2f);
                if (chaserPos != null)
                {
                    var chaser = Spawn(component.ChaserPrototype, chaserPos.Value);

                    if (TryComp<HierophantChaserComponent>(chaser, out var chaserComp))
                    {
                        chaserComp.Target = target;
                        chaserComp.MoveInterval = isLowHealth ? 0.15f : 0.3f;
                        chaserComp.NextMoveTime = _timing.CurTime + TimeSpan.FromSeconds(chaserComp.MoveInterval);
                    }
                }
            });

            spawnDelay += 0.3f;
        }
    }

    private void PerformDamageAreaAttack(EntityUid uid, HierophantBossComponent component, bool isLowHealth)
    {
        var selfCoords = Transform(uid).Coordinates;
        var maxRadius = isLowHealth ? 8 : 6;

        for (int wave = 1; wave <= maxRadius; wave++)
        {
            var currentWave = wave;
            Timer.Spawn((int)((wave - 1) * 0.6 * 1000), () =>
            {
                if (Deleted(uid)) return;
                SpawnDamageWave(selfCoords, currentWave);
            });
        }
    }

    #endregion

    #region Pattern Spawning

    private void SpawnCrossLines(EntityCoordinates center, int length, bool diagonal)
    {
        var directions = diagonal ?
            new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) } :
            new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) };

        foreach (var dir in directions)
        {
            SpawnDamageTile(center);
            for (int i = 1; i <= length; i++)
            {
                var offset = dir * i;
                var spawnCoords = center.Offset(offset);

                if (CanSpawnAt(spawnCoords))
                {
                    SpawnDamageTile(spawnCoords);
                }
            }
        }
    }

    private void SpawnDamageWave(EntityCoordinates center, int wave)
    {
        var size = wave * 2 - 1;
        var halfSize = (size - 1) / 2;

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                if (Math.Abs(x) == halfSize || Math.Abs(y) == halfSize)
                {
                    var spawnCoords = center.Offset(new Vector2(x, y));

                    if (CanSpawnAt(spawnCoords))
                    {
                        SpawnDamageTile(spawnCoords);
                    }
                }
            }
        }
    }

    private void Spawn3x3Area(EntityCoordinates center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var spawnCoords = center.Offset(new Vector2(x, y));
                if (CanSpawnAt(spawnCoords))
                {
                    SpawnDamageTile(spawnCoords);
                }
            }
        }
    }

    private void SpawnDamageTile(EntityCoordinates coords)
    {
        Spawn(SpawnPrototype, coords);
    }

    #endregion

    #region Utility Methods

    private EntityCoordinates? FindBlinkPosition(EntityCoordinates targetCoords)
    {
        for (int i = 0; i < 10; i++)
        {
            var offset = new Vector2(_random.Next(-3, 4), _random.Next(-3, 4));
            var testCoords = targetCoords.Offset(offset);
            var correctedCoords = GetTileCenter(testCoords);

            if (CanSpawnAt(correctedCoords))
                return correctedCoords;
        }
        return null;
    }

    private EntityCoordinates? FindSpawnPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 5; i++)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var distance = _random.NextFloat(1f, maxDistance);
            var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;

            var testCoords = center.Offset(offset);

            if (CanSpawnAt(testCoords))
                return testCoords;
        }
        return center;
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

    private bool IsLowHealth(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return false;

        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return false;

        return damageable.TotalDamage / threshold >= LowHealthThreshold;
    }

    private float GetHealthRatio(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 1f;

        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 1f;

        return 1f - (float)(double)(damageable.TotalDamage / threshold);
    }

    private float GetAttackSpeedMultiplier(float healthRatio)
    {
        return Math.Max(1.0f, 3.0f - healthRatio * 2f);
    }

    #endregion

    #region Chaser System

    private void OnChaserStartup(EntityUid uid, HierophantChaserComponent component, ComponentStartup args)
    {
        component.CurrentSteps = 0;
        component.NextMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.MoveInterval);
    }

    private void UpdateChasers(float frameTime)
    {
        var query = EntityQueryEnumerator<HierophantChaserComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextMoveTime)
                continue;

            if (comp.Target == null || !Exists(comp.Target.Value))
            {
                QueueDel(uid);
                continue;
            }

            if (comp.CurrentSteps >= comp.MaxChaseSteps)
            {
                QueueDel(uid);
                continue;
            }

            var chaserPos = Transform(uid).Coordinates;
            var targetPos = Transform(comp.Target.Value).Coordinates;

            var nextPos = FindNextChaserPosition(chaserPos, targetPos);
            if (nextPos != null)
            {
                _transform.SetCoordinates(uid, nextPos.Value);
                comp.CurrentSteps++;
                SpawnDamageTile(chaserPos);
            }

            comp.NextMoveTime = _timing.CurTime + TimeSpan.FromSeconds(comp.MoveInterval);
        }
    }

    private EntityCoordinates? FindNextChaserPosition(EntityCoordinates current, EntityCoordinates target)
    {
        var direction = (target.Position - current.Position).Normalized();
        var nextPos = current.Offset(direction);

        return CanSpawnAt(nextPos) ? nextPos : null;
    }

    #endregion

    #region Death Handling

    private void OnHierophantKilled(EntityUid uid, HierophantBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        Spawn(component.RewardProto, coords);

        QueueDel(uid);
    }

    #endregion
}
