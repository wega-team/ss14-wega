using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class IceDemonSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IceDemonComponent, IceDemonIceShotActionEvent>(OnIceShot);
        SubscribeLocalEvent<IceDemonComponent, IceDemonTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<IceDemonComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnIceShot(Entity<IceDemonComponent> ent, ref IceDemonIceShotActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var demon = ent.Owner;
        var target = args.Target;

        var shooterCoords = Transform(demon).Coordinates;
        var targetPos = Transform(target).Coordinates.Position;
        var shooterPos = shooterCoords.Position;

        var direction = (targetPos - shooterPos).Normalized();

        var projectile = Spawn(args.BoltPrototype, shooterCoords);
        _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, demon,
            SharedGunSystem.ProjectileSpeed / 3);
    }

    private void OnTeleport(Entity<IceDemonComponent> ent, ref IceDemonTeleportActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var demon = ent.Owner;
        var target = args.Target;
        var radius = args.TeleportRadius;

        var targetCoords = Transform(target).Coordinates;

        var teleportPos = FindTeleportPosition(targetCoords, radius);
        if (teleportPos == null)
            return;

        _audio.PlayPvs(args.BlinkSound, demon);

        _transform.SetCoordinates(demon, teleportPos.Value);
    }

    private void OnDamage(Entity<IceDemonComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (ent.Comp.AfterimagesSpawned)
            return;

        var healthRatio = GetHealthRatio(ent);
        if (healthRatio > ent.Comp.HealthThreshold)
            return;

        SpawnAfterimages(ent);
        ent.Comp.AfterimagesSpawned = true;
    }

    private void SpawnAfterimages(Entity<IceDemonComponent> ent)
    {
        var demon = ent.Owner;
        var coords = Transform(demon).Coordinates;
        var count = ent.Comp.AfterimageCount;

        for (int i = 0; i < count; i++)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos == null)
                continue;

            Spawn(ent.Comp.AfterimagePrototype, spawnPos.Value);
        }
    }

    #region Utility Methods

    private EntityCoordinates? FindTeleportPosition(EntityCoordinates targetCoords, float radius)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(1f, radius);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

            var testCoords = targetCoords.Offset(offset);
            var corrected = GetTileCenter(testCoords);

            if (CanSpawnAt(corrected))
                return corrected;
        }

        for (int attempt = 0; attempt < 15; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(0.5f, 3f);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

            var testCoords = targetCoords.Offset(offset);
            var corrected = GetTileCenter(testCoords);

            if (CanSpawnAt(corrected))
                return corrected;
        }

        return null;
    }

    private EntityCoordinates? FindSpawnPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 10; i++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(0.5f, maxDistance);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

            var testCoords = center.Offset(offset);
            if (CanSpawnAt(testCoords))
                return testCoords;
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

    private float GetHealthRatio(EntityUid uid)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 1f;

        return 1f - (float)(totalDamage / threshold.Value);
    }

    #endregion
}
