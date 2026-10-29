using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Projectiles;

public sealed partial class ProjectileTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileTeleportComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileTeleportComponent component, ref ProjectileHitEvent ev)
    {
        if (component.Used || ev.Shooter == null)
            return;

        var shooter = ev.Shooter.Value;
        var targetCoords = Transform(ev.Target).Coordinates;

        var safePosition = FindSafeTeleportPosition(targetCoords);
        if (safePosition != null)
        {
            _transform.SetCoordinates(shooter, safePosition.Value);
        }
        else
        {
            _transform.SetCoordinates(shooter, targetCoords);
        }

        if (component.UseOnCollide)
            component.Used = true;
    }

    #region Utility Methods

    private EntityCoordinates? FindSafeTeleportPosition(EntityCoordinates targetCoords)
    {
        if (CanSpawnAt(targetCoords))
            return targetCoords;

        for (int radius = 1; radius <= 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;

                    var offset = new Vector2(dx, dy);
                    var testCoords = targetCoords.Offset(offset);
                    var correctedCoords = GetTileCenter(testCoords);

                    if (CanSpawnAt(correctedCoords))
                        return correctedCoords;
                }
            }
        }

        for (int i = 0; i < 20; i++)
        {
            var offset = new Vector2(_random.Next(-8, 9), _random.Next(-8, 9));
            var testCoords = targetCoords.Offset(offset);
            var correctedCoords = GetTileCenter(testCoords);

            if (CanSpawnAt(correctedCoords))
                return correctedCoords;
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
