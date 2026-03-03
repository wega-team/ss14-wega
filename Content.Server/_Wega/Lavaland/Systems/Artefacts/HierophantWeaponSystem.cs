using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Interaction;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class HierophantClubSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantClubComponent, AfterInteractEvent>(OnWeaponAfterInteract);
    }

    private void OnWeaponAfterInteract(EntityUid uid, HierophantClubComponent component, AfterInteractEvent args)
    {
        if (_delay.IsDelayed(uid))
            return;

        _delay.TryResetDelay(uid);

        var user = args.User;
        if (args.Target != null && HasComp<MobStateComponent>(args.Target))
        {
            PerformChaserAttack(component, user, args.Target.Value);
            return;
        }

        var userTransform = Transform(user);
        var lookDirection = _transform.GetWorldRotation(userTransform).ToWorldVec();
        var userCoords = userTransform.Coordinates;
        var targetCoords = args.ClickLocation;

        var toTarget = targetCoords.Position - userCoords.Position;
        if (Vector2.Dot(lookDirection, toTarget.Normalized()) > 0)
        {
            PerformDamageAreaAttack(component, targetCoords);
        }
        else
        {
            PerformCrossAttack(component, targetCoords);
        }
    }

    private void PerformChaserAttack(HierophantClubComponent component, EntityUid user, EntityUid target)
    {
        var userCoords = Transform(user).Coordinates;

        var chasersToSpawn = Math.Min(component.MaxChasers, _random.Next(1, 3));
        for (int i = 0; i < chasersToSpawn; i++)
        {
            var chaserPos = FindSpawnPositionNear(userCoords, 2f);
            if (chaserPos != null)
            {
                var chaser = Spawn(component.ChaserPrototype, chaserPos.Value);

                if (TryComp<HierophantChaserComponent>(chaser, out var chaserComp))
                {
                    chaserComp.Target = target;
                    chaserComp.MoveInterval = 0.3f;
                }
            }
        }
    }

    private void PerformCrossAttack(HierophantClubComponent component, EntityCoordinates targetCoords)
    {
        var lineLength = component.CrossLength;

        var useDiagonal = _random.Next(2) == 0;
        SpawnCrossLines(component, targetCoords, lineLength, useDiagonal);
    }

    public void PerformDamageAreaAttack(HierophantClubComponent component, EntityCoordinates targetCoords)
    {
        Spawn3x3Area(component, targetCoords);
    }

    #region Utility Methods

    private void SpawnCrossLines(HierophantClubComponent component, EntityCoordinates center, int length, bool diagonal)
    {
        var directions = diagonal ?
            new[] { new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1) } :
            new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) };

        foreach (var dir in directions)
        {
            SpawnDamageTile(component, center);
            for (int i = 1; i <= length; i++)
            {
                var offset = dir * i;
                var spawnCoords = center.Offset(offset);

                if (CanSpawnAt(spawnCoords))
                {
                    SpawnDamageTile(component, spawnCoords);
                }
            }
        }
    }

    private void Spawn3x3Area(HierophantClubComponent component, EntityCoordinates center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var spawnCoords = center.Offset(new Vector2(x, y));
                if (CanSpawnAt(spawnCoords))
                {
                    SpawnDamageTile(component, spawnCoords);
                }
            }
        }
    }

    private void SpawnDamageTile(HierophantClubComponent component, EntityCoordinates coords)
        => Spawn(component.DamageTilePrototype, coords);

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

    #endregion
}
