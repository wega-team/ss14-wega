using System.Numerics;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class IceWhelpSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IceWhelpLineBreathActionEvent>(OnLineBreath);
        SubscribeLocalEvent<IceWhelpCircleBreathActionEvent>(OnCircleBreath);
    }

    private void OnLineBreath(IceWhelpLineBreathActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(args.Performer) || !Exists(args.Target))
            return;

        var whelp = args.Performer;
        var target = args.Target;

        var shooterCoords = Transform(whelp).Coordinates;
        var targetPos = Transform(target).Coordinates.Position;
        var shooterPos = shooterCoords.Position;

        var direction = (targetPos - shooterPos).Normalized();

        for (int i = 1; i <= args.LineLength; i++)
        {
            var offset = direction * i;
            var spawnCoords = shooterCoords.Offset(offset);

            if (!CanSpawnAt(spawnCoords))
                continue;

            var delay = (i - 1) * 0.2f;
            var currentOffset = offset;
            var currentCoords = spawnCoords;

            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Exists(whelp) || _mobState.IsDead(whelp))
                    return;

                if (!Exists(target) || _mobState.IsIncapacitated(target))
                    return;

                var projectile = Spawn(args.BoltPrototype, currentCoords);
                _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, whelp,
                    SharedGunSystem.ProjectileSpeed / 2);
            });
        }
    }

    private void OnCircleBreath(IceWhelpCircleBreathActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(args.Performer))
            return;

        var whelp = args.Performer;

        var shooterCoords = Transform(whelp).Coordinates;
        var shooterPos = shooterCoords.Position;
        var mapUid = _transform.GetMap(whelp);
        if (mapUid == null)
            return;

        var count = args.ProjectileCount;
        var radius = args.Radius;

        for (int i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathF.PI * 2;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var spawnPos = shooterPos + direction * radius;
            var spawnCoords = new EntityCoordinates(mapUid.Value, spawnPos);

            if (!CanSpawnAt(spawnCoords))
                continue;

            var projectile = Spawn(args.BoltPrototype, spawnCoords);
            _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, whelp,
                SharedGunSystem.ProjectileSpeed / 2);
        }
    }

    #region Utility Methods

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
