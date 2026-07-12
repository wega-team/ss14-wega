using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class PandoraSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private NPCUseActionsOnTargetSystem _npcActions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PandoraComponent, MapInitEvent>(OnPandoraMapInit);
        SubscribeLocalEvent<PandoraComponent, DamageChangedEvent>(OnPandoraDamage);

        SubscribeLocalEvent<PandoraComponent, PandoraBlastLineActionEvent>(OnBlastLine);
        SubscribeLocalEvent<PandoraComponent, PandoraMagicBoxActionEvent>(OnMagicBox);
        SubscribeLocalEvent<PandoraComponent, PandoraTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<PandoraComponent, PandoraAOEBlastActionEvent>(OnAOEBlast);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdatePassiveMovement();
    }

    private void OnPandoraMapInit(Entity<PandoraComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.PassiveMoveInterval);
    }

    private void OnPandoraDamage(Entity<PandoraComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        UpdateAttackSpeed(ent);
    }

    private void UpdateAttackSpeed(Entity<PandoraComponent> ent)
    {
        var healthRatio = GetHealthRatio(ent);
        var speedMultiplier = Math.Max(1.0f, 3.0f - healthRatio * 2f);

        _npcActions.SetDelaySpeed(ent, Math.Max(0.5f, Math.Min(1.0f, 1.0f / speedMultiplier)));
    }

    private float GetHealthRatio(EntityUid uid)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 1f;

        return 1f - (float)(totalDamage / threshold.Value);
    }

    private void UpdatePassiveMovement()
    {
        var query = EntityQueryEnumerator<PandoraComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var pandora, out var htn))
        {
            if (_timing.CurTime < pandora.NextPassiveMoveTime)
                continue;

            EntityUid? target = null;
            if (htn.Blackboard.TryGetValue<EntityUid>(pandora.TargetKey, out var targetUid, EntityManager))
                target = targetUid;

            if (target != null && Exists(target.Value))
                MoveTowardsTarget((uid, pandora), target.Value);

            pandora.NextPassiveMoveTime = _timing.CurTime + TimeSpan.FromSeconds(pandora.PassiveMoveInterval);
        }
    }

    private void MoveTowardsTarget(Entity<PandoraComponent> ent, EntityUid target)
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

    private void OnBlastLine(Entity<PandoraComponent> ent, ref PandoraBlastLineActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var targetCoords = Transform(args.Target).Coordinates;
        var directions = new List<Vector2>
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };
        var dir = _random.Pick(directions).Normalized();

        for (int i = 1; i <= args.LineLength; i++)
        {
            var offset = dir * i;
            var spawnCoords = targetCoords.Offset(offset);
            if (CanSpawnAt(spawnCoords))
                Spawn(ent.Comp.SquarePrototype, spawnCoords);
        }
    }

    private void OnMagicBox(Entity<PandoraComponent> ent, ref PandoraMagicBoxActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var targetCoords = Transform(args.Target).Coordinates;
        int half = args.BoxSize / 2;
        int safeHalf = args.SafeZoneSize / 2;

        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                if (Math.Abs(x) <= safeHalf && Math.Abs(y) <= safeHalf)
                    continue;

                var offset = new Vector2(x, y);
                var spawnCoords = targetCoords.Offset(offset);
                if (CanSpawnAt(spawnCoords))
                    Spawn(ent.Comp.SquarePrototype, spawnCoords);
            }
        }
    }

    private void OnTeleport(Entity<PandoraComponent> ent, ref PandoraTeleportActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var target = args.Target;
        var selfCoords = Transform(ent).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        Spawn3x3(ent, selfCoords);

        var blinkPos = FindBlinkPosition(targetCoords);
        if (blinkPos == null)
            return;

        if (!Exists(ent) || _mobState.IsDead(ent))
            return;

        _transform.SetCoordinates(ent, blinkPos.Value);
        Spawn3x3(ent, blinkPos.Value);
    }

    private void Spawn3x3(Entity<PandoraComponent> ent, EntityCoordinates center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var offset = new Vector2(x, y);
                var spawnCoords = center.Offset(offset);
                if (CanSpawnAt(spawnCoords))
                    Spawn(ent.Comp.SquarePrototype, spawnCoords);
            }
        }
    }

    private void OnAOEBlast(Entity<PandoraComponent> ent, ref PandoraAOEBlastActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var selfCoords = Transform(ent).Coordinates;
        int radius = args.Radius;

        for (int wave = 1; wave <= radius; wave++)
        {
            var currentWave = wave;
            Timer.Spawn(TimeSpan.FromSeconds((wave - 1) * 0.3f), () =>
            {
                if (!Exists(ent))
                    return;

                SpawnWaveRing(ent, selfCoords, currentWave);
            });
        }
    }

    private void SpawnWaveRing(Entity<PandoraComponent> ent, EntityCoordinates center, int wave)
    {
        int half = wave;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                if (Math.Abs(x) == half || Math.Abs(y) == half)
                {
                    var offset = new Vector2(x, y);
                    var spawnCoords = center.Offset(offset);
                    if (CanSpawnAt(spawnCoords))
                        Spawn(ent.Comp.SquarePrototype, spawnCoords);
                }
            }
        }
    }

    #region Utility Methods

    private EntityCoordinates? FindBlinkPosition(EntityCoordinates targetCoords)
    {
        for (int i = 0; i < 10; i++)
        {
            var offset = new Vector2(_random.Next(-3, 4), _random.Next(-3, 4));
            var testCoords = targetCoords.Offset(offset);
            var corrected = GetTileCenter(testCoords);
            if (CanSpawnAt(corrected))
                return corrected;
        }
        return null;
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
