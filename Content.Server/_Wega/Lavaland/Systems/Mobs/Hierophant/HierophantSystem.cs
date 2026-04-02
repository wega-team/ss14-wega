using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Achievements;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

/// <summary>
/// Do you also look at history and it's like there was some kind of great battle?
/// </summary>
public sealed partial class HierophantSystem : EntitySystem
{
    [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly NPCUseActionsOnTargetSystem _npcActions = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    private const float LowHealthThreshold = 0.5f;
    private static readonly EntProtoId SpawnPrototype = "EffectHierophantSquare";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantBossComponent, MegafaunaKilledEvent>(OnHierophantKilled);

        SubscribeLocalEvent<HierophantBossComponent, MapInitEvent>(OnHierophantMapInit);
        SubscribeLocalEvent<HierophantBossComponent, DamageChangedEvent>(OnHierophantDamage);

        SubscribeLocalEvent<HierophantBossComponent, HierophantBlinkActionEvent>(OnBlinkAction);
        SubscribeLocalEvent<HierophantBossComponent, HierophantCrossActionEvent>(OnCrossAction);
        SubscribeLocalEvent<HierophantBossComponent, HierophantChaserActionEvent>(OnChaserAction);
        SubscribeLocalEvent<HierophantBossComponent, HierophantDamageAreaActionEvent>(OnDamageAreaAction);

        SubscribeLocalEvent<HierophantChaserComponent, ComponentStartup>(OnChaserStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateChasers();
        UpdatePassiveMovement();
        UpdateReturnToBase();
    }

    private void OnHierophantKilled(EntityUid uid, HierophantBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        foreach (var reward in component.RewardsProto)
            Spawn(reward, coords);

        QueueDel(uid);
        if (args.Killer != null)
        {
            _achievement.QueueAchievement(args.Killer.Value, AchievementsEnum.HierophantBoss);
        }
    }

    #region Passive Movement

    private void OnHierophantMapInit(EntityUid uid, HierophantBossComponent component, MapInitEvent args)
    {
        component.HomePosition = Transform(uid).Coordinates;
        component.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.PassiveMoveInterval);
        component.NextReturnCheckTime = _timing.CurTime + TimeSpan.FromMinutes(component.ReturnCheckInterval);
    }

    private void UpdatePassiveMovement()
    {
        var query = EntityQueryEnumerator<MegafaunaComponent, HierophantBossComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var mega, out var component, out var htn))
        {
            if (_timing.CurTime < component.NextPassiveMoveTime)
                continue;

            EntityUid? target = null;
            if (htn.Blackboard.TryGetValue<EntityUid>(mega.TargetKey, out var targetUid, EntityManager))
                target = targetUid;

            if (target != null && Exists(target.Value))
            {
                MoveTowardsNearestTarget((uid, component), target.Value);
            }

            component.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.PassiveMoveInterval);
        }
    }

    private void UpdateReturnToBase()
    {
        var query = EntityQueryEnumerator<MegafaunaComponent, HierophantBossComponent>();
        while (query.MoveNext(out var uid, out var mega, out var component))
        {
            if (_timing.CurTime < component.NextReturnCheckTime)
                continue;

            CheckReturnToBase(uid, component, mega.TargetKey);
            component.NextReturnCheckTime = _timing.CurTime + TimeSpan.FromMinutes(component.ReturnCheckInterval);
        }
    }

    private void CheckReturnToBase(EntityUid uid, HierophantBossComponent component, string targetKey)
    {
        if (TryComp<HTNComponent>(uid, out var htn))
        {
            EntityUid? target = null;
            if (htn.Blackboard.TryGetValue<EntityUid>(targetKey, out var targetUid, EntityManager))
                target = targetUid;

            if (target != null && Exists(target.Value))
                return;
        }

        if (!component.NeedComeBack)
            return;

        ReturnToBase(uid, component);
    }

    private void ReturnToBase(EntityUid uid, HierophantBossComponent component)
    {
        var currentPos = Transform(uid).Coordinates;

        Spawn3x3Area(currentPos);

        _transform.SetCoordinates(uid, component.HomePosition);
        _audio.PlayPvs(component.BlinkSound, Transform(uid).Coordinates);
        component.NeedComeBack = false;

        Spawn3x3Area(component.HomePosition);
    }

    private void MoveTowardsNearestTarget(Entity<HierophantBossComponent> ent, EntityUid target)
    {
        var selfCoords = Transform(ent).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        var direction = (targetCoords.Position - selfCoords.Position).Normalized();
        var newCoords = selfCoords.Offset(direction);

        var correctedCoords = GetTileCenter(newCoords);

        if (CanSpawnAt(correctedCoords))
        {
            _transform.SetCoordinates(ent, correctedCoords);
            _audio.PlayPvs(ent.Comp.BlinkSound, Transform(ent).Coordinates);
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

    #region Damage System

    private void OnHierophantDamage(EntityUid uid, HierophantBossComponent component, DamageChangedEvent args)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (args.DamageIncreased && totalDamage > 0)
        {
            UpdateAttackSpeed(uid);

            if (!component.NeedComeBack)
                component.NeedComeBack = true;
        }
    }

    private void UpdateAttackSpeed(EntityUid uid)
    {
        var healthRatio = GetHealthRatio(uid);
        var speedMultiplier = GetAttackSpeedMultiplier(healthRatio);

        _npcActions.SetDelaySpeed(uid, Math.Max(0.5f, Math.Min(1.0f, 1.0f / speedMultiplier)));
    }

    #endregion

    #region Action Handlers

    private void OnBlinkAction(Entity<HierophantBossComponent> ent, ref HierophantBlinkActionEvent args)
    {
        args.Handled = true;

        var isLowHealth = IsLowHealth(ent);
        PerformBlinkAttack(ent, args.Target, isLowHealth);
    }

    private void OnCrossAction(Entity<HierophantBossComponent> ent, ref HierophantCrossActionEvent args)
    {
        args.Handled = true;

        var isLowHealth = IsLowHealth(ent);
        PerformCrossAttack(ent, args.Target, isLowHealth, args.CrossLength);
    }

    private void OnChaserAction(Entity<HierophantBossComponent> ent, ref HierophantChaserActionEvent args)
    {
        args.Handled = true;

        var isLowHealth = IsLowHealth(ent);
        PerformChaserAttack(ent, args.Target, isLowHealth, args.ChaserCount, args.ChaserDelay);
    }

    private void OnDamageAreaAction(Entity<HierophantBossComponent> ent, ref HierophantDamageAreaActionEvent args)
    {
        args.Handled = true;

        var isLowHealth = IsLowHealth(ent);
        PerformDamageAreaAttack(ent, isLowHealth, args.MaxRadius, args.WaveDelay);
    }

    #endregion

    #region Attack Implementations

    private void PerformBlinkAttack(Entity<HierophantBossComponent> ent, EntityUid target, bool isLowHealth)
    {
        var selfCoords = Transform(ent).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        Spawn3x3Area(selfCoords);

        var blinkPos = FindBlinkPosition(targetCoords);
        if (blinkPos != null)
        {
            _transform.SetCoordinates(ent, blinkPos.Value);
            _audio.PlayPvs(ent.Comp.BlinkSound, Transform(ent).Coordinates);
            Spawn3x3Area(blinkPos.Value);
        }

        if (isLowHealth)
        {
            Timer.Spawn(TimeSpan.FromMilliseconds(500), () =>
            {
                if (!Exists(ent)) return;

                var newBlinkPos = FindBlinkPosition(targetCoords);
                if (newBlinkPos != null)
                {
                    _transform.SetCoordinates(ent, newBlinkPos.Value);
                    _audio.PlayPvs(ent.Comp.BlinkSound, Transform(ent).Coordinates);
                    Spawn3x3Area(newBlinkPos.Value);
                }
            });
        }
    }

    private void PerformCrossAttack(Entity<HierophantBossComponent> ent, EntityUid target, bool isLowHealth, int lineLength)
    {
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

    private void PerformChaserAttack(Entity<HierophantBossComponent> ent, EntityUid target, bool isLowHealth, int baseCount, float spawnDelay)
    {
        var selfCoords = Transform(ent).Coordinates;

        var extraCount = isLowHealth ? 1 : 0;
        var chasersToSpawn = baseCount + extraCount;

        var currentDelay = 0f;
        for (int i = 0; i < chasersToSpawn; i++)
        {
            var delay = currentDelay;
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Exists(ent)) return;

                var chaserPos = FindSpawnPositionNear(selfCoords, 2f);
                if (chaserPos != null)
                {
                    var chaser = Spawn(ent.Comp.ChaserPrototype, chaserPos.Value);

                    if (TryComp<HierophantChaserComponent>(chaser, out var chaserComp))
                    {
                        chaserComp.Target = target;
                        chaserComp.MoveInterval = isLowHealth ? 0.15f : 0.3f;
                        chaserComp.NextMoveTime = _timing.CurTime + TimeSpan.FromSeconds(chaserComp.MoveInterval);
                    }
                }
            });

            currentDelay += spawnDelay;
        }
    }

    private void PerformDamageAreaAttack(Entity<HierophantBossComponent> ent, bool isLowHealth, int maxRadius, float waveDelay)
    {
        var selfCoords = Transform(ent).Coordinates;
        var radius = isLowHealth ? maxRadius : Math.Max(3, maxRadius / 2);

        for (int wave = 1; wave <= radius; wave++)
        {
            var currentWave = wave;
            Timer.Spawn(TimeSpan.FromSeconds((wave - 1) * waveDelay), () =>
            {
                if (!Exists(ent)) return;
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

    private bool IsLowHealth(Entity<HierophantBossComponent> ent)
    {
        var totalDamage = _damage.GetTotalDamage(ent.Owner);
        if (!_threshold.TryGetThresholdForState(ent, MobState.Dead, out var threshold))
            return false;

        return totalDamage >= threshold - threshold * LowHealthThreshold;
    }

    private float GetHealthRatio(EntityUid uid)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 1f;

        return 1f - (float)(double)(totalDamage / threshold);
    }

    private float GetAttackSpeedMultiplier(float healthRatio)
        => Math.Max(1.0f, 3.0f - healthRatio * 2f);

    #endregion

    #region Chaser System

    private void OnChaserStartup(EntityUid uid, HierophantChaserComponent component, ComponentStartup args)
    {
        component.CurrentSteps = 0;
        component.NextMoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.MoveInterval);
    }

    private void UpdateChasers()
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
}
