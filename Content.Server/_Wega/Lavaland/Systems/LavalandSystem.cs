using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Server.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Gravity;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Content.Shared.Light.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Pinpointer;
using Content.Shared.Radio.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandSystem : SharedLavalandSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly GridFixtureSystem _fixture = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedShuttleSystem _iff = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationLavalandComponent, StationPostInitEvent>(OnStationStartup, before: [typeof(ConditionalSpawnerSystem)]);
    }

    #region Lavaland Procesing
    /*
        You've changed 8...... times, and now only the best version of you remains.
     */

    private void OnStationStartup(Entity<StationLavalandComponent> ent, ref StationPostInitEvent args)
    {
        if (!_cfg.GetCVar(WegaCVars.LavalandEnabled) || !ent.Comp.Enabled)
        {
            Log.Info("Lavaland processing is currently disabled.");
            return;
        }

        AddLavaland(ent);
    }

    private void AddLavaland(Entity<StationLavalandComponent> ent)
    {
        var planetProto = _random.Pick(ent.Comp.Planets);
        if (!_proto.TryIndex(planetProto, out var planet))
        {
            Log.Error($"Unable lavaland planet prototype '{planetProto}'");
            return;
        }

        var mapUid = _map.CreateMap(out var mapId);
        if (!_loader.TryLoadGrid(mapId, ent.Comp.LavalandAvanpostPath, out var avanpost, offset: Vector2.Zero))
        {
            Log.Error($"Unable to load lavaland avanpost map {ent.Comp.LavalandAvanpostPath} for {ToPrettyString(ent)}");
            _map.DeleteMap(mapId);
            return;
        }

        _meta.SetEntityName(mapUid, Loc.GetString($"{planet.ID.ToLower()}-map"));
        _meta.SetEntityName(avanpost.Value, Loc.GetString($"{planet.ID.ToLower()}-map-avanpost"));
        var avanpostComp = EnsureComp<LavalandAvanpostComponent>(avanpost.Value);
        EnsureComp<ActiveRadioComponent>(avanpost.Value).Channels.Add(avanpostComp.AnnouncementChannel);
        EnsureComp<ProtectedGridComponent>(avanpost.Value);
        var navMap = EnsureComp<NavMapComponent>(avanpost.Value);
        _navMap.RefreshGridWithOffset(avanpost.Value.Owner, navMap, avanpost.Value.Comp, Vector2.Zero);

        EnsureComp<ProtectedGridComponent>(mapUid);
        var grid = EnsureComp<MapGridComponent>(mapUid); // For build processing after creating planet
        EnsureComp<NavMapComponent>(mapUid);

        _map.CreateMap(out var tempMapId);

        var worldAABBs = new HashSet<Box2>();
        GenerateBuildings(mapId, tempMapId, mapUid, planet, ref worldAABBs);

        _biome.EnsurePlanet(mapUid, _proto.Index(planet.Biome), ent.Comp.Seed, mapLight: planet.MapLightColor);
        var lightCycle = EnsureComp<LightCycleComponent>(mapUid);
        lightCycle.MaxLightLevel = planet.MaxLightLevel;
        lightCycle.MinLightLevel = planet.MinLightLevel;
        Dirty(mapUid, lightCycle);

        var biome = EnsureComp<BiomeComponent>(mapUid);
        foreach (var layer in planet.BiomeLayers)
        {
            _biome.AddMarkerLayer(mapUid, biome, layer);
        }

        PreloadAvanpostArea(mapUid, avanpost.Value, biome, grid);

        // Pre-loading of tiles in merged grids
        foreach (var worldAABB in worldAABBs)
        {
            var tiles = new List<(Vector2i Index, Tile Tile)>();
            _biome.ReserveTiles(mapUid, worldAABB, tiles, biome, grid);
        }

        var lava = EnsureComp<LavalandComponent>(mapUid);
        lava.NextWeatherChange = _gameTiming.CurTime + TimeSpan.FromMinutes(_random.Next(5, 15));
        lava.PlanetPrototype = planetProto;

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        for (var i = 0; i < Atmospherics.TotalNumberOfGases && i < planet.GasesContent.Count(); i++)
            moles[i] = planet.GasesContent[i];

        var mixture = new GasMixture(moles, planet.AtmosphereTemperature);
        _atmos.SetMapAtmosphere(mapUid, false, mixture);

        var affectedQuery = EntityQueryEnumerator<TransformComponent, GravityAffectedComponent>();
        while (affectedQuery.MoveNext(out var uid, out var transform, out var affected))
        {
            if (transform.MapUid != mapUid)
                continue;

            _gravity.RefreshWeightless((uid, affected));
        }
    }

    public void GenerateBuildings(MapId mapId, MapId tempMapId, EntityUid mainGrid, LavalandPlanetPrototype planet, ref HashSet<Box2> worldAABBs)
    {
        var buildings = _proto.EnumeratePrototypes<LavalandBuildingPrototype>();
        var buildingList = buildings.Where(b => b.CurrentPlanet == planet.ID)
            .Select(b => new { Building = b, RandomValue = _random.Next() })
            .OrderByDescending(x => x.Building.IgnoringCounting).ThenByDescending(x => x.Building.ExactPosition.HasValue)
            .ThenByDescending(x => x.Building.ApproximatePosition.HasValue).ThenBy(x => x.RandomValue)
            .Select(x => x.Building).ToList();

        var maxBuildings = _cfg.GetCVar(WegaCVars.LavalandMaxBuildings);
        var minDistanceBetween = _cfg.GetCVar(WegaCVars.LavalandBuildingsDistance);
        var occupiedAreas = new List<Box2>();

        var spawned = 0;
        foreach (var building in buildingList)
        {
            if (!building.IgnoringCounting && spawned >= maxBuildings)
                continue;

            if (TryFindValidPosition(building, occupiedAreas, minDistanceBetween, 12, out var position))
            {
                var offsetIndex = occupiedAreas.Count;
                SpawnBuilding(mapId, tempMapId, mainGrid, building, 200 * offsetIndex, position, ref worldAABBs);

                var lastAABB = worldAABBs.Last();
                occupiedAreas.Add(lastAABB);
                if (!building.IgnoringCounting)
                    spawned++;
            }
        }

        _map.DeleteMap(tempMapId);
    }

    private bool TryFindValidPosition(LavalandBuildingPrototype proto, List<Box2> occupiedAreas,
        float minDistance, int maxAttempts, out Vector2 position)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (proto.ExactPosition.HasValue)
            {
                position = proto.ExactPosition.Value;
            }
            else if (proto.ApproximatePosition.HasValue)
            {
                var min = proto.ApproximatePosition.Value.Min;
                var max = proto.ApproximatePosition.Value.Max;

                var angle = _random.NextAngle();
                var distance = _random.NextFloat(min, max);
                position = angle.ToVec() * distance;
            }
            else
            {
                var min = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMin);
                var max = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMax);

                var angle = _random.NextAngle();
                var distance = _random.NextFloat(min, max);
                position = angle.ToVec() * distance;
            }

            position = new Vector2((int)position.X, (int)position.Y);

            var tempBounds = new Box2(-4, -4, 4, 4).Translated(position);

            bool tooClose = false;
            foreach (var occupiedArea in occupiedAreas)
            {
                var expandedArea = occupiedArea.Enlarged(minDistance);
                if (expandedArea.Intersects(tempBounds))
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return true;
        }

        position = Vector2.Zero;
        return false;
    }

    private void SpawnBuilding(MapId mapId, MapId tempMapId, EntityUid mainGrid, LavalandBuildingPrototype proto,
        int offset, Vector2 position, ref HashSet<Box2> worldAABBs)
    {
        var opts = new DeserializationOptions() { PauseMaps = true };

        var offsetPos = new Vector2(0, offset);
        var alignedPosition = new Vector2((int)position.X, (int)position.Y);
        if (!_loader.TryLoadGrid(proto.MergeWithPlanet ? tempMapId : mapId, proto.GridPath, out var buildingGrid, opts, offsetPos))
        {
            Log.Error($"Failed to load lavaland building {proto.ID} at {position}");
            return;
        }

        if (proto.MergeWithPlanet && mainGrid != buildingGrid.Value.Owner)
        {
            if (proto.PreloadingArea)
            {
                worldAABBs.Add(buildingGrid.Value.Comp.LocalAABB.Translated(alignedPosition));
            }
            MergeWithPlanet(mainGrid, buildingGrid.Value.Owner, alignedPosition);
        }
        else
        {
            _iff.AddIFFFlag(buildingGrid.Value, IFFFlags.HideLabel);
            EnsureComp<GridLavalandWeatherProtectionComponent>(buildingGrid.Value);
            EnsureComp<ProtectedGridComponent>(buildingGrid.Value);
            var navMap = EnsureComp<NavMapComponent>(buildingGrid.Value);
            _navMap.RefreshGridWithOffset(buildingGrid.Value.Owner, navMap, buildingGrid.Value.Comp, alignedPosition);

            _transform.SetCoordinates(buildingGrid.Value, new EntityCoordinates(mainGrid, position));
            worldAABBs.Add(buildingGrid.Value.Comp.LocalAABB.Translated(alignedPosition));
        }

        Log.Debug($"Loaded lavaland building {proto.ID} at {position}");
    }

    private void MergeWithPlanet(EntityUid mainGridUid, EntityUid buildingGridUid, Vector2 offsetPosition)
    {
        if (!TryComp<MapGridComponent>(mainGridUid, out var mainGrid) ||
            !TryComp<MapGridComponent>(buildingGridUid, out var buildingGrid))
        {
            Log.Error($"Cannot merge grids: components missing");
            return;
        }

        try
        {
            var relativeRotation = _transform.GetWorldRotation(buildingGridUid) - _transform.GetWorldRotation(mainGridUid);

            var offset = new Vector2i((int)offsetPosition.X, (int)offsetPosition.Y);
            if (HasComp<DecalGridComponent>(buildingGridUid))
            {
                var decalBounds = buildingGrid.LocalAABB;
                var decals = _decals.GetDecalsIntersecting(buildingGridUid, decalBounds);
                foreach (var (_, decal) in decals)
                {
                    var newPos = new Vector2(decal.Coordinates.X, decal.Coordinates.Y) + offsetPosition;
                    _decals.TryAddDecal(decal.Id, new EntityCoordinates(mainGridUid, newPos),
                        out _, decal.Color, decal.Angle, decal.ZIndex, decal.Cleanable);
                }
            }

            var anchoredEnts = new HashSet<EntityUid>();
            var allChildren = new HashSet<EntityUid>();
            GetAllChildren(Transform(buildingGridUid), allChildren);

            foreach (var child in allChildren)
            {
                if (Transform(child).Anchored && !HasComp<CableComponent>(child))
                    anchoredEnts.Add(child);
            }

            foreach (var ent in anchoredEnts)
            {
                if (Transform(ent).Anchored)
                    _transform.Unanchor(ent);
            }

            _fixture.Merge(mainGridUid, buildingGridUid, offset, relativeRotation, mainGrid, buildingGrid);

            foreach (var ent in anchoredEnts)
            {
                if (!ent.IsValid() || Deleted(ent))
                    continue;

                _transform.AnchorEntity(ent);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to merge grids: {ex.Message}");
        }
    }

    private void GetAllChildren(TransformComponent transform, HashSet<EntityUid> result)
    {
        var enumerator = transform.ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!child.IsValid())
                continue;

            result.Add(child);
            GetAllChildren(Transform(child), result);
        }
        enumerator.Dispose();
    }

    private void PreloadAvanpostArea(EntityUid mapUid, EntityUid avanpostUid, BiomeComponent biome, MapGridComponent grid)
    {
        if (!TryComp<MapGridComponent>(avanpostUid, out var avanpostGrid))
            return;

        var worldPos = _transform.GetWorldPosition(avanpostUid);
        var localBounds = avanpostGrid.LocalAABB;

        var center = worldPos + localBounds.Center;
        var radius = Math.Max(localBounds.Width, localBounds.Height) / 2f + 6f;

        _biome.ReserveTilesInCircle(mapUid, center, radius, biome, grid);
    }

    #endregion
}
