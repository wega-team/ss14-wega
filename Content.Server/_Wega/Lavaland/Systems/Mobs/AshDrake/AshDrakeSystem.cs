using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Visuals;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class AshDrakeSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private Dictionary<EntityUid, LavaArenaData> _activeArenas = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AshDrakeBossComponent, AshDrakeConeFireActionEvent>(OnConeFireAction);
        SubscribeLocalEvent<AshDrakeBossComponent, AshDrakeBreathingFireActionEvent>(OnBreathingFireAction);
        SubscribeLocalEvent<AshDrakeBossComponent, AshDrakeLavaActionEvent>(OnLavaAction);
    }

    #region Cone Fire
    private void OnConeFireAction(Entity<AshDrakeBossComponent> ent, ref AshDrakeConeFireActionEvent args)
    {
        args.Handled = true;
        if (_random.NextDouble() < 0.5)
            StartMeteorShower(ent);

        var drakeWorldPos = _transform.GetWorldPosition(ent);
        var targetWorldPos = _transform.GetWorldPosition(args.Target);

        var direction = (targetWorldPos - drakeWorldPos).Normalized();

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var mainShotPos = drakeWorldPos + direction * 10f;
        var mainCoordinates = new EntityCoordinates(mapUid.Value, mainShotPos);
        ShootAt(ent, mainCoordinates);

        var angle = MathF.PI / 6;

        var cosAngle = MathF.Cos(angle);
        var sinAngle = MathF.Sin(angle);
        var dirRight = new Vector2(
            direction.X * cosAngle - direction.Y * sinAngle,
            direction.X * sinAngle + direction.Y * cosAngle
        ).Normalized();

        var dirLeft = new Vector2(
            direction.X * cosAngle + direction.Y * sinAngle,
            -direction.X * sinAngle + direction.Y * cosAngle
        ).Normalized();

        var rightShotPos = drakeWorldPos + dirRight * 10f;
        var rightCoordinates = new EntityCoordinates(mapUid.Value, rightShotPos);
        ShootAt(ent, rightCoordinates);

        var leftShotPos = drakeWorldPos + dirLeft * 10f;
        var leftCoordinates = new EntityCoordinates(mapUid.Value, leftShotPos);
        ShootAt(ent, leftCoordinates);

        PlayAttackSound(ent);
    }
    #endregion

    #region Breathing Fire
    private void OnBreathingFireAction(Entity<AshDrakeBossComponent> ent, ref AshDrakeBreathingFireActionEvent args)
    {
        args.Handled = true;
        if (_random.NextDouble() < 0.5)
            StartMeteorShower(ent);

        var totalDamage = _damage.GetTotalDamage(ent.Owner);
        if (totalDamage > 0 && _threshold.TryGetThresholdForState(ent, MobState.Dead, out var threshold))
        {
            if (totalDamage >= threshold - threshold * args.HealthModifier)
            {
                ShootCircularPattern(ent, 12, 1);
                return;
            }
        }

        var drakeWorldPos = _transform.GetWorldPosition(ent);
        var targetWorldPos = _transform.GetWorldPosition(args.Target);

        var direction = (targetWorldPos - drakeWorldPos).Normalized();

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var shotPos = drakeWorldPos + direction * 12f;
        var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);
        ShootAt(ent, shotCoordinates);

        PlayAttackSound(ent);
    }
    #endregion

    #region Lava Action
    private void OnLavaAction(Entity<AshDrakeBossComponent> ent, ref AshDrakeLavaActionEvent args)
    {
        args.Handled = true;

        var totalDamage = _damage.GetTotalDamage(ent.Owner);
        if (_threshold.TryGetThresholdForState(ent, MobState.Dead, out var threshold))
        {
            if (totalDamage >= threshold - threshold * args.HealthModifier)
            {
                StartLavaArena(ent, args);
                return;
            }
        }
        StartLavaJump(ent, args);
    }

    #region Lava Arena
    private void StartLavaArena(Entity<AshDrakeBossComponent> ent, AshDrakeLavaActionEvent args)
    {
        var targetWorldPos = _transform.GetWorldPosition(args.Target);

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        if (!IsAreaClearForArena(mapUid.Value, targetWorldPos, 6))
        {
            StartLavaJump(ent, args);
            return;
        }

        _appearance.SetData(ent.Owner, VisualLayers.Enabled, true);
        EnsureComp<GodmodeComponent>(ent);
        _npc.SleepNPC(ent.Owner);

        var arenaData = new LavaArenaData
        {
            ArenaCenter = targetWorldPos,
            ArenaSize = 5,
            CurrentPhase = 0,
            TotalPhases = 3,
            Drake = ent.Owner,
            Args = args
        };

        var shadowCoords = new EntityCoordinates(mapUid.Value, targetWorldPos);
        arenaData.Shadow = Spawn(ent.Comp.Shadow, shadowCoords);

        CreateFireWalls(mapUid.Value, targetWorldPos, 6, args.Wall, arenaData);

        _activeArenas[ent.Owner] = arenaData;
        Timer.Spawn(TimeSpan.FromSeconds(2f), () =>
        {
            if (Exists(ent.Owner) && _activeArenas.ContainsKey(ent.Owner))
                StartArenaPhase(ent);
        });
    }

    private bool IsAreaClearForArena(EntityUid mapUid, Vector2 centerPos, int arenaSize)
    {
        var halfSize = arenaSize / 2;
        var worldAABB = new Box2(
            new Vector2(centerPos.X - halfSize, centerPos.Y - halfSize),
            new Vector2(centerPos.X + halfSize, centerPos.Y + halfSize)
        );

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                var testPos = centerPos + new Vector2(x, y);
                if (!IsValidMapPosition(mapUid, testPos))
                    return false;
            }
        }
        var coordinates = new EntityCoordinates(mapUid, centerPos);
        var entitiesInArea = _lookup.GetEntitiesInRange(coordinates, halfSize, LookupFlags.Static);

        foreach (var entity in entitiesInArea)
        {
            if (_activeArenas.Values.Any(data => data.Walls.Contains(entity)))
                continue;

            return false;
        }

        return true;
    }

    private void CreateFireWalls(EntityUid mapUid, Vector2 centerPos, int size, EntProtoId wallProto, LavaArenaData arenaData)
    {
        var halfSize = size / 2;

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                if (Math.Abs(x) != halfSize && Math.Abs(y) != halfSize)
                    continue;

                var wallPos = centerPos + new Vector2(x, y);
                var wallCoords = new EntityCoordinates(mapUid, wallPos);

                if (IsValidMapPosition(mapUid, wallPos))
                {
                    var wall = Spawn(wallProto, wallCoords);
                    arenaData.Walls.Add(wall);
                }
            }
        }
    }

    private void StartArenaPhase(Entity<AshDrakeBossComponent> drakeUid)
    {
        if (!_activeArenas.TryGetValue(drakeUid, out var arenaData) || arenaData.Args == null)
            return;

        arenaData.CurrentPhase++;

        if (arenaData.CurrentPhase > arenaData.TotalPhases)
        {
            StartDrakeLandingAfterArena(drakeUid, arenaData);
            return;
        }

        var mapUid = _transform.GetMap(drakeUid.Owner);
        if (mapUid == null)
            return;

        var safePos = GetRandomArenaPosition(arenaData.ArenaCenter, 5, 2, arenaData.PreviousSafePos);
        arenaData.PreviousSafePos = safePos;

        var safeCoords = new EntityCoordinates(mapUid.Value, safePos);
        var safeMarker = Spawn(arenaData.Args.SafeMarker, safeCoords);
        arenaData.SafeMarkers.Add(safeMarker);

        Timer.Spawn(TimeSpan.FromSeconds(1.5f), () =>
        {
            if (!_activeArenas.ContainsKey(drakeUid) || !Exists(drakeUid))
                return;

            CreateArenaLava(drakeUid, arenaData, safePos);
        });

        CheckPlayerEscape(drakeUid, arenaData);
    }

    private void StartDrakeLandingAfterArena(Entity<AshDrakeBossComponent> drakeUid, LavaArenaData arenaData)
    {
        CleanupArenaObjects(arenaData);

        var mapUid = _transform.GetMap(drakeUid.Owner);
        if (mapUid == null || arenaData.Args == null)
        {
            FinalizeArenaCompletion(drakeUid, arenaData);
            return;
        }

        var markerCoords = new EntityCoordinates(mapUid.Value, arenaData.ArenaCenter);
        var marker = Spawn(arenaData.Args.Marker, markerCoords);
        arenaData.LandingMarker = marker;

        Timer.Spawn(TimeSpan.FromSeconds(1f), () =>
        {
            if (!_activeArenas.ContainsKey(drakeUid) || !Exists(drakeUid))
                return;

            CompleteArenaLanding(drakeUid, arenaData);
        });
    }

    private void CompleteArenaLanding(Entity<AshDrakeBossComponent> drakeUid, LavaArenaData arenaData)
    {
        if (arenaData.Args == null)
            return;

        var mapUid = _transform.GetMap(drakeUid.Owner);
        if (mapUid == null)
            return;

        if (Exists(arenaData.LandingMarker))
            QueueDel(arenaData.LandingMarker);

        var centerCoords = new EntityCoordinates(mapUid.Value, arenaData.ArenaCenter);
        _transform.SetCoordinates(drakeUid.Owner, centerCoords);

        ApplyCameraShakeOnLanding(drakeUid, arenaData.ArenaCenter);
        ApplyLandingDamage(drakeUid, arenaData.ArenaCenter, arenaData.Args);
        CreateLavaPool(drakeUid, arenaData.ArenaCenter, arenaData.Args);

        FinalizeArenaCompletion(drakeUid, arenaData);
    }

    private void CleanupArenaObjects(LavaArenaData arenaData)
    {
        foreach (var wall in arenaData.Walls)
            if (Exists(wall)) QueueDel(wall);
        arenaData.Walls.Clear();

        foreach (var marker in arenaData.SafeMarkers)
            if (Exists(marker)) QueueDel(marker);
        arenaData.SafeMarkers.Clear();

        foreach (var lava in arenaData.LavaTiles)
            if (Exists(lava)) QueueDel(lava);
        arenaData.LavaTiles.Clear();
    }

    private void FinalizeArenaCompletion(Entity<AshDrakeBossComponent> drakeUid, LavaArenaData arenaData)
    {
        if (Exists(arenaData.Shadow))
            QueueDel(arenaData.Shadow);

        if (Exists(arenaData.LandingMarker))
            QueueDel(arenaData.LandingMarker);

        _activeArenas.Remove(drakeUid.Owner);

        _appearance.SetData(drakeUid.Owner, VisualLayers.Enabled, false);
        RemCompDeferred<GodmodeComponent>(drakeUid.Owner);
        _npc.WakeNPC(drakeUid.Owner);
        if (arenaData.Args != null)
        {
            SetHTNTarget(drakeUid, arenaData.Args.Target);
        }

        PlayAttackSound(drakeUid);
    }

    private Vector2 GetRandomArenaPosition(Vector2 center, int arenaSize, int minDistanceFromPrevious, Vector2? previousPos)
    {
        var halfSize = arenaSize / 2;
        Vector2 safePos;
        int attempts = 0;

        do
        {
            var x = _random.Next(-halfSize, halfSize + 1);
            var y = _random.Next(-halfSize, halfSize + 1);
            safePos = center + new Vector2(x, y);

            attempts++;

            if (previousPos.HasValue)
            {
                var distance = Vector2.Distance(safePos, previousPos.Value);
                if (distance <= minDistanceFromPrevious && attempts < 20)
                    continue;
            }

            break;

        } while (true);

        return safePos;
    }

    private void CreateArenaLava(Entity<AshDrakeBossComponent> drakeUid, LavaArenaData arenaData, Vector2 safePos)
    {
        var mapUid = _transform.GetMap(drakeUid.Owner);
        if (mapUid == null || arenaData.Args == null)
            return;

        var halfArena = arenaData.ArenaSize / 2;
        for (int x = -halfArena; x <= halfArena; x++)
        {
            for (int y = -halfArena; y <= halfArena; y++)
            {
                var lavaPos = arenaData.ArenaCenter + new Vector2(x, y);
                if (Vector2.Distance(lavaPos, safePos) < 0.5f)
                    continue;

                var lavaCoords = new EntityCoordinates(mapUid.Value, lavaPos);

                if (IsValidMapPosition(mapUid.Value, lavaPos))
                {
                    var lava = Spawn(arenaData.Args.LavaLess, lavaCoords);
                    arenaData.LavaTiles.Add(lava);
                }
            }
        }

        Timer.Spawn(TimeSpan.FromSeconds(1.5f), () =>
        {
            if (!_activeArenas.ContainsKey(drakeUid))
                return;

            arenaData.LavaTiles.Clear();
            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
            {
                if (Exists(drakeUid) && _activeArenas.ContainsKey(drakeUid))
                    StartArenaPhase(drakeUid);
            });
        });
    }

    private void CheckPlayerEscape(Entity<AshDrakeBossComponent> drakeUid, LavaArenaData arenaData)
    {
        if (arenaData.Args == null)
            return;

        var target = arenaData.Args.Target;
        if (!Exists(target))
            return;

        var targetPos = _transform.GetWorldPosition(target);
        var distanceFromCenter = Vector2.Distance(targetPos, arenaData.ArenaCenter);
        var arenaRadius = arenaData.ArenaSize / 2f + 0.5f;

        if (arenaData.CurrentPhase < arenaData.TotalPhases && distanceFromCenter > arenaRadius)
        {
            EndLavaArena(drakeUid, arenaData.Args, false);
        }
        else
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
            {
                if (Exists(drakeUid) && _activeArenas.ContainsKey(drakeUid))
                    CheckPlayerEscape(drakeUid, arenaData);
            });
        }
    }

    private void EndLavaArena(Entity<AshDrakeBossComponent> ent, AshDrakeLavaActionEvent args, bool completedNormally)
    {
        if (!_activeArenas.TryGetValue(ent.Owner, out var arenaData))
            return;

        CleanupArenaObjects(arenaData);

        if (Exists(arenaData.Shadow)) QueueDel(arenaData.Shadow);
        if (Exists(arenaData.LandingMarker)) QueueDel(arenaData.LandingMarker);

        _activeArenas.Remove(ent.Owner);

        if (!completedNormally)
        {
            _damage.TryChangeDamage(ent.Owner, args.HealingSpec, true, false);

            var mapUid = _transform.GetMap(ent.Owner);
            if (mapUid != null)
            {
                var centerCoords = new EntityCoordinates(mapUid.Value, arenaData.ArenaCenter);
                _transform.SetCoordinates(ent.Owner, centerCoords);
            }

            _appearance.SetData(ent.Owner, VisualLayers.Enabled, false);
            RemCompDeferred<GodmodeComponent>(ent);
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, args.Target);

            ShootCircularPattern(ent, 20, 1);
        }

        PlayAttackSound(ent);
    }
    #endregion

    #region Lava Jump
    private void StartLavaJump(Entity<AshDrakeBossComponent> ent, AshDrakeLavaActionEvent args)
    {
        _appearance.SetData(ent.Owner, VisualLayers.Enabled, true);
        EnsureComp<GodmodeComponent>(ent);
        _npc.SleepNPC(ent.Owner);

        var drakeWorldPos = _transform.GetWorldPosition(ent);
        var targetWorldPos = _transform.GetWorldPosition(args.Target);

        var direction = (targetWorldPos - drakeWorldPos).Normalized();

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
        {
            EndLavaJump(ent, args);
            return;
        }

        var landingPos = CalculateSafeLandingPosition(mapUid.Value, drakeWorldPos, direction, args.Target);

        var shadowCoords = new EntityCoordinates(mapUid.Value, drakeWorldPos);
        var shadow = Spawn(ent.Comp.Shadow, shadowCoords);

        var markerCoords = new EntityCoordinates(mapUid.Value, landingPos);
        Timer.Spawn(TimeSpan.FromSeconds(1.5f), () =>
        {
            if (Exists(shadow) && Exists(ent.Owner))
                Spawn(args.Marker, markerCoords);
        });

        StartShadowMovement(ent, shadow, drakeWorldPos, landingPos, args);
    }

    private Vector2 CalculateSafeLandingPosition(EntityUid mapUid, Vector2 startPos, Vector2 direction, EntityUid target)
    {
        var targetWorldPos = _transform.GetWorldPosition(target);
        var distanceToTarget = Vector2.Distance(startPos, targetWorldPos);

        float maxDistance;
        if (distanceToTarget < 1f)
        {
            maxDistance = 2f;
        }
        else if (distanceToTarget < 4f)
        {
            maxDistance = 4f;
        }
        else
        {
            maxDistance = 6f;
        }

        for (float distance = maxDistance; distance >= 1f; distance -= 0.5f)
        {
            var testPos = startPos + direction * distance;
            if (IsPositionSafeForLanding(mapUid, testPos))
                return testPos;
        }

        return startPos + direction * 1f;
    }

    private bool IsPositionSafeForLanding(EntityUid mapUid, Vector2 position)
    {
        if (!IsValidMapPosition(mapUid, position))
            return false;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var testPos = position + new Vector2(x, y);
                if (x == 0 && y == 0)
                    continue;

                if (!IsValidMapPosition(mapUid, testPos))
                    return false;
            }
        }

        return true;
    }

    private void StartShadowMovement(Entity<AshDrakeBossComponent> ent, EntityUid shadow,
        Vector2 startPos, Vector2 endPos, AshDrakeLavaActionEvent args)
    {
        var totalFlightTime = 2.5f;
        var steps = 25;
        var stepTime = totalFlightTime / steps;

        var currentPos = startPos;
        var direction = (endPos - startPos).Normalized();
        var totalDistance = Vector2.Distance(startPos, endPos);
        var stepDistance = totalDistance / steps;

        for (int i = 1; i <= steps; i++)
        {
            var step = i;
            Timer.Spawn(TimeSpan.FromSeconds(step * stepTime), () =>
            {
                if (!Exists(shadow) || !Exists(ent.Owner))
                    return;

                var progress = (float)step / steps;
                var currentStepPos = startPos + (endPos - startPos) * progress;

                var mapUid = _transform.GetMap(ent.Owner);
                if (mapUid != null)
                {
                    var shadowCoords = new EntityCoordinates(mapUid.Value, currentStepPos);
                    _transform.SetCoordinates(shadow, shadowCoords);
                }

                CreateLavaTrail(ent, currentStepPos, args);

                if (step == steps)
                {
                    Timer.Spawn(TimeSpan.FromSeconds(0.1f), () =>
                    {
                        CompleteLavaJump(ent, shadow, endPos, args);
                    });
                }
            });
        }
    }

    private void CreateLavaTrail(Entity<AshDrakeBossComponent> ent, Vector2 centerPos, AshDrakeLavaActionEvent args)
    {
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var lavaPos = centerPos + new Vector2(x, y);
                if (!IsValidMapPosition(mapUid.Value, lavaPos))
                    continue;

                if (x == 0 && y == 0)
                {
                    var lavaCoords = new EntityCoordinates(mapUid.Value, lavaPos);
                    Spawn(args.Lava, lavaCoords);
                }
                else if (_random.NextDouble() < 0.6f)
                {
                    var lavaCoords = new EntityCoordinates(mapUid.Value, lavaPos);
                    Spawn(args.Lava, lavaCoords);
                }
            }
        }
    }

    private void CompleteLavaJump(Entity<AshDrakeBossComponent> ent, EntityUid shadow, Vector2 landingPos, AshDrakeLavaActionEvent args)
    {
        if (Exists(shadow)) QueueDel(shadow);

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid != null)
        {
            if (IsPositionSafeForLanding(mapUid.Value, landingPos))
            {
                var landingCoords = new EntityCoordinates(mapUid.Value, landingPos);
                _transform.SetCoordinates(ent.Owner, landingCoords);

                ApplyCameraShakeOnLanding(ent, landingPos);
                ApplyLandingDamage(ent, landingPos, args);
                CreateLavaPool(ent, landingPos, args);
            }
            else
            {
                var safeLandingPos = FindNearestSafePosition(mapUid.Value, landingPos);
                var safeCoords = new EntityCoordinates(mapUid.Value, safeLandingPos);
                _transform.SetCoordinates(ent.Owner, safeCoords);

                ApplyCameraShakeOnLanding(ent, safeLandingPos);
                ApplyLandingDamage(ent, safeLandingPos, args);
                CreateLavaPool(ent, safeLandingPos, args);
            }
        }

        Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
        {
            EndLavaJump(ent, args);
        });
    }

    private Vector2 FindNearestSafePosition(EntityUid mapUid, Vector2 originalPos)
    {
        for (int radius = 0; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                        continue;

                    var testPos = originalPos + new Vector2(x, y);
                    if (IsPositionSafeForLanding(mapUid, testPos))
                        return testPos;
                }
            }
        }

        return originalPos;
    }

    private void ApplyCameraShakeOnLanding(Entity<AshDrakeBossComponent> ent, Vector2 landingPos)
    {
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var landingCoords = new EntityCoordinates(mapUid.Value, landingPos);

        var nearbyRadius = 6f;
        var nearbyEntities = _lookup.GetEntitiesInRange<ActorComponent>(landingCoords, nearbyRadius);

        foreach (var entity in nearbyEntities)
        {
            ApplyCameraShake(entity.Owner, 0.3f);
        }

        var damageRadius = 1.5f;
        var directHitEntities = _lookup.GetEntitiesInRange<ActorComponent>(landingCoords, damageRadius, LookupFlags.Uncontained);
        foreach (var entity in directHitEntities)
        {
            if (entity.Owner == ent.Owner)
                continue;

            ApplyStrongShake(entity.Owner);
        }
    }

    private void ApplyCameraShake(EntityUid playerUid, float intensity)
    {
        var direction = _random.NextAngle().ToVec();
        var shakeMagnitude = intensity * 0.5f;

        var kickback = direction * shakeMagnitude;
        _recoil.KickCamera(playerUid, kickback);
    }

    private void ApplyStrongShake(EntityUid playerUid)
    {
        var direction = _random.NextAngle().ToVec();
        var strongKick = direction * _random.NextFloat(0.4f, 0.8f);

        Timer.Spawn(TimeSpan.FromSeconds(_random.NextFloat(0.1f, 0.5f)), () =>
        {
            _recoil.KickCamera(playerUid, strongKick);
        });
    }

    private void ApplyLandingDamage(Entity<AshDrakeBossComponent> ent, Vector2 centerPos, AshDrakeLavaActionEvent args)
    {
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var damageRadius = 1.5f;
        var landingCoords = new EntityCoordinates(mapUid.Value, centerPos);

        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(landingCoords, damageRadius, LookupFlags.Uncontained);
        foreach (var entity in entities)
        {
            if (entity.Owner == ent.Owner)
                continue;

            if (_mobState.IsIncapacitated(entity.Owner))
            {
                _gibbing.Gib(entity.Owner); // The End
                continue;
            }

            _damage.TryChangeDamage(entity.Owner, args.LandingDamage);
        }
    }

    private void CreateLavaPool(Entity<AshDrakeBossComponent> ent, Vector2 centerPos, AshDrakeLavaActionEvent args)
    {
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var lavaPos = centerPos + new Vector2(x, y);
                var lavaCoords = new EntityCoordinates(mapUid.Value, lavaPos);
                if (IsValidMapPosition(mapUid.Value, lavaPos))
                    Spawn(args.Lava, lavaCoords);
            }
        }
    }

    private void EndLavaJump(Entity<AshDrakeBossComponent> ent, AshDrakeLavaActionEvent args)
    {
        _appearance.SetData(ent.Owner, VisualLayers.Enabled, false);
        RemCompDeferred<GodmodeComponent>(ent);
        _npc.WakeNPC(ent.Owner);
        SetHTNTarget(ent, args.Target);

        PlayAttackSound(ent);
    }
    #endregion
    #endregion

    #region Utility
    private void StartMeteorShower(Entity<AshDrakeBossComponent> ent)
    {
        var drakeWorldPos = _transform.GetWorldPosition(ent);

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        const int radius = 9;
        const float cellChance = 0.11f;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                var distance = MathF.Sqrt(x * x + y * y);
                if (distance > radius)
                    continue;

                if (_random.NextDouble() < cellChance)
                {
                    var cellPos = drakeWorldPos + new Vector2(x, y);
                    if (IsValidMapPosition(mapUid.Value, cellPos))
                    {
                        var cellCoordinates = new EntityCoordinates(mapUid.Value, cellPos);
                        Spawn(ent.Comp.MeteorCircle, cellCoordinates);
                    }
                }
            }
        }

        PlayAttackSound(ent);
    }

    private bool IsValidMapPosition(EntityUid mapUid, Vector2 position)
    {
        var coordinates = new EntityCoordinates(mapUid, position);

        var gridUid = _transform.GetGrid(coordinates);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coordinates);
        if (!_map.TryGetTileRef(gridUid.Value, grid, tilePos, out var tileRef))
            return false;

        return !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable);
    }

    private void ShootCircularPattern(Entity<AshDrakeBossComponent> ent, int projectilesPerWave, float waveDelay)
    {
        var drakeWorldPos = _transform.GetWorldPosition(ent);

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        for (int wave = 0; wave < 3; wave++)
        {
            var waveTimer = wave * waveDelay;

            Timer.Spawn(TimeSpan.FromSeconds(waveTimer), () =>
            {
                if (!Exists(ent.Owner))
                    return;

                for (int i = 0; i < projectilesPerWave; i++)
                {
                    var angle = i * (MathF.PI * 2 / projectilesPerWave);
                    var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                    var distance = 10f;
                    var shotPos = drakeWorldPos + direction * distance;
                    var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

                    var intraWaveDelay = i * 0.05f;
                    Timer.Spawn(TimeSpan.FromSeconds(intraWaveDelay), () =>
                    {
                        if (Exists(ent.Owner))
                        {
                            ShootAt(ent.Owner, shotCoordinates);
                        }
                    });
                }

                PlayAttackSound(ent);
            });
        }
    }

    private void SetHTNTarget(Entity<AshDrakeBossComponent> boss, EntityUid target)
    {
        if (!TryComp<HTNComponent>(boss, out var htn))
            return;

        if (htn.Blackboard.TryGetValue<EntityUid>(boss.Comp.TargetKey, out var targetEnt, EntityManager) && Exists(targetEnt))
            return;

        htn.Blackboard.SetValue(boss.Comp.TargetKey, target);
    }

    private void ShootAt(EntityUid drake, EntityCoordinates targetCoordinates)
    {
        if (!TryComp<GunComponent>(drake, out var gun))
            return;

        _gun.ForceShoot(drake, drake, gun, targetCoordinates);
    }
    #endregion

    private void PlayAttackSound(Entity<AshDrakeBossComponent> ent)
        => _audio.PlayPvs(ent.Comp.AttackSound, ent);
}

public sealed class LavaArenaData
{
    public Vector2 ArenaCenter;
    public int ArenaSize;
    public int CurrentPhase;
    public int TotalPhases;
    public EntityUid Drake;
    public EntityUid Shadow;
    public EntityUid? LandingMarker;
    public AshDrakeLavaActionEvent? Args;
    public Vector2? PreviousSafePos;
    public List<EntityUid> Walls = new();
    public List<EntityUid> SafeMarkers = new();
    public List<EntityUid> LavaTiles = new();
}
