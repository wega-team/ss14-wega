using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class BroodmotherSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BroodmotherComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<BroodmotherComponent, BroodmotherTentaclePatchActionEvent>(OnTentaclePatch);
        SubscribeLocalEvent<BroodmotherComponent, BroodmotherRageActionEvent>(OnRage);
        SubscribeLocalEvent<BroodmotherComponent, BroodmotherSpawnChildrenActionEvent>(OnSpawnChildren);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateRageState();
    }

    private void OnInit(Entity<BroodmotherComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(ent, out var speedMod))
            return;

        ent.Comp.BaseSprintSpeed = speedMod.BaseSprintSpeed;
        ent.Comp.BaseWalkSpeed = speedMod.BaseWalkSpeed;
    }

    private void OnTentaclePatch(Entity<BroodmotherComponent> ent, ref BroodmotherTentaclePatchActionEvent args)
    {
        args.Handled = true;
        if (!Exists(args.Target))
            return;

        var targetCoords = Transform(args.Target).Coordinates;

        SpawnSquare(targetCoords, args.PatchSize, args.TentaclePrototype);
        SpawnCross(targetCoords, args.CrossLength, args.TentaclePrototype);
    }

    private void SpawnSquare(EntityCoordinates center, int size, EntProtoId prototype)
    {
        int half = (size - 1) / 2;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                var offset = new Vector2(x, y);
                var spawnCoords = center.Offset(offset);
                if (CanSpawnAt(spawnCoords))
                    Spawn(prototype, spawnCoords);
            }
        }
    }

    private void SpawnCross(EntityCoordinates center, int length, EntProtoId prototype)
    {
        int half = (length - 1) / 2;
        for (int x = -half; x <= half; x++)
        {
            var offset = new Vector2(x, 0);
            var spawnCoords = center.Offset(offset);
            if (CanSpawnAt(spawnCoords))
                Spawn(prototype, spawnCoords);
        }
        for (int y = -half; y <= half; y++)
        {
            if (y == 0) continue;
            var offset = new Vector2(0, y);
            var spawnCoords = center.Offset(offset);
            if (CanSpawnAt(spawnCoords))
                Spawn(prototype, spawnCoords);
        }
    }

    private void OnRage(Entity<BroodmotherComponent> ent, ref BroodmotherRageActionEvent args)
    {
        args.Handled = true;
        if (ent.Comp.IsRaging)
            return;

        ent.Comp.IsRaging = true;
        ent.Comp.RageEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.RageDuration);
        ent.Comp.PostRageSlowEndTime = ent.Comp.RageEndTime + TimeSpan.FromSeconds(args.PostRageSlowDuration);

        ApplySpeedMultiplier(ent.Owner, ent.Comp, args.SpeedMultiplier);
        _appearance.SetData(ent, VisualLayers.Enabled, true);
    }

    private void UpdateRageState()
    {
        var query = EntityQueryEnumerator<BroodmotherComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsRaging && _timing.CurTime >= comp.RageEndTime)
            {
                comp.IsRaging = false;
                comp.IsPostRageSlow = true;
                ApplySpeedMultiplier(uid, comp, 0.5f);
                _appearance.SetData(uid, VisualLayers.Enabled, false);
                comp.PostRageSlowEndTime = _timing.CurTime + TimeSpan.FromSeconds(7f);
            }
            else if (comp.IsPostRageSlow && _timing.CurTime >= comp.PostRageSlowEndTime)
            {
                comp.IsPostRageSlow = false;
                ApplySpeedMultiplier(uid, comp, 1f);
            }
        }
    }

    private void ApplySpeedMultiplier(EntityUid uid, BroodmotherComponent comp, float multiplier)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var speedMod))
            return;

        _movementSpeed.ChangeBaseSpeed(uid, comp.BaseWalkSpeed * multiplier, comp.BaseSprintSpeed * multiplier, speedMod.BaseAcceleration);
    }

    private void OnSpawnChildren(Entity<BroodmotherComponent> ent, ref BroodmotherSpawnChildrenActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || _mobState.IsIncapacitated(args.Target))
            return;

        var currentChildren = GetCurrentChildCount(ent);
        if (currentChildren >= args.MaxChildren)
            return;

        var coords = Transform(ent).Coordinates;
        var toSpawn = Math.Min(args.ChildCount, args.MaxChildren - currentChildren);

        for (int i = 0; i < toSpawn; i++)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos == null)
                continue;

            var child = Spawn(args.ChildPrototype, spawnPos.Value);
            EnsureComp<GoliathChildComponent>(child).Mother = ent.Owner;
        }
    }

    private int GetCurrentChildCount(Entity<BroodmotherComponent> ent)
    {
        var count = 0;
        var query = EntityQueryEnumerator<GoliathChildComponent>();
        while (query.MoveNext(out var childUid, out var childComp))
        {
            if (childComp.Mother == ent.Owner && !_mobState.IsDead(childUid))
                count++;
        }
        return count;
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
        return null;
    }
}
