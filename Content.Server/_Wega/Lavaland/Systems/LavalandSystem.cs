using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Server.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Events;
using Content.Shared.Atmos;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Decals;
using Content.Shared.Lavaland.Components;
using Content.Shared.Maps;
using Content.Shared.Pinpointer;
using Content.Shared.Radio.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tiles;
using Content.Shared.Weather;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GridFixtureSystem _fixture = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedShuttleSystem _iff = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly ProtoId<DamageTypePrototype> StructuralDamage = "Structural";
    private static readonly ProtoId<DamageTypePrototype> CausticDamage = "Caustic";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationLavalandComponent, StationPostInitEvent>(OnStationStartup);
    }

    #region Weather Procesing

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LavalandComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextWeatherChange < _gameTiming.CurTime && !comp.WarningSent)
                SendWeatherWarning(uid, comp);

            if (comp.WeatherStartTime < _gameTiming.CurTime && comp.WarningSent && comp.CurrentWeatherType == LavalandWeatherType.None)
                StartWeather(uid, comp);

            if (comp.CurrentWeatherType != LavalandWeatherType.None)
                ProcessWeather(uid, comp, frameTime);
        }
    }

    private void SendWeatherWarning(EntityUid uid, LavalandComponent comp)
    {
        var weatherType = GetRandomWeatherType();
        var (weatherProto, _) = GetWeatherInfo(weatherType);

        comp.UpcomingWeatherType = weatherType;
        comp.UpcomingWeatherProto = weatherProto;
        comp.WeatherStartTime = _gameTiming.CurTime + TimeSpan.FromSeconds(60);
        comp.WarningSent = true;

        SendWeatherAlert(weatherType);
    }

    private void StartWeather(EntityUid uid, LavalandComponent comp)
    {
        comp.CurrentWeatherType = comp.UpcomingWeatherType;
        comp.CurrentWeatherProto = comp.UpcomingWeatherProto;
        comp.CurrentWeatherEnd = _gameTiming.CurTime + GetWeatherInfo(comp.CurrentWeatherType).duration;

        comp.UpcomingWeatherType = LavalandWeatherType.None;
        comp.UpcomingWeatherProto = null;
        comp.WarningSent = false;

        comp.NextWeatherChange = comp.CurrentWeatherEnd + TimeSpan.FromMinutes(_random.Next(5, 15));

        if (comp.CurrentWeatherProto != null)
        {
            _weather.SetWeather(Transform(uid).MapID, _proto.Index(comp.CurrentWeatherProto.Value), comp.CurrentWeatherEnd);
        }
    }

    private void ProcessWeather(EntityUid uid, LavalandComponent comp, float frameTime)
    {
        if (comp.CurrentWeatherEnd < _gameTiming.CurTime)
        {
            EndWeather(uid, comp);
            return;
        }

        comp.DamageTick -= frameTime;
        if (comp.DamageTick <= 0f)
        {
            ApplyWeatherDamage(uid, comp);
            comp.DamageTick = GetDamageInterval(comp.CurrentWeatherType);
            if (comp.CurrentWeatherType == LavalandWeatherType.VolcanicActivity)
                ApplyVolcanicActivity();
        }
    }

    private void EndWeather(EntityUid uid, LavalandComponent comp)
    {
        var endedWeather = comp.CurrentWeatherType;

        comp.CurrentWeatherType = LavalandWeatherType.None;
        comp.CurrentWeatherProto = null;
        comp.DamageTick = 0f;

        _weather.SetWeather(Transform(uid).MapID, null, null);

        SendWeatherEndAlert(endedWeather);
    }

    private LavalandWeatherType GetRandomWeatherType()
    {
        var roll = _random.Next(100);

        if (roll < 40) return LavalandWeatherType.StormWind;
        if (roll < 70) return LavalandWeatherType.AshStormLight;
        if (roll < 85) return LavalandWeatherType.AshStormHeavy;
        if (roll < 95) return LavalandWeatherType.VolcanicActivity;
        return LavalandWeatherType.AcidRain;
    }

    private (ProtoId<WeatherPrototype>? proto, TimeSpan duration) GetWeatherInfo(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => ("AshfallLight", TimeSpan.FromSeconds(_random.Next(60, 120))),
            LavalandWeatherType.AshStormHeavy => ("AshfallHeavy", TimeSpan.FromSeconds(_random.Next(90, 150))),
            LavalandWeatherType.VolcanicActivity => (null, TimeSpan.FromSeconds(_random.Next(60, 120))),
            LavalandWeatherType.AcidRain => ("Fallout", TimeSpan.FromSeconds(_random.Next(60, 120))),
            LavalandWeatherType.StormWind => (null, TimeSpan.FromSeconds(_random.Next(60, 120))),
            _ => (null, TimeSpan.Zero)
        };
    }

    private float GetDamageInterval(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => 5f,
            LavalandWeatherType.AshStormHeavy => 3.33f,
            LavalandWeatherType.AcidRain => 1.5f,
            _ => 5f
        };
    }

    private void ApplyWeatherDamage(EntityUid lavalandUid, LavalandComponent comp)
    {
        var query = EntityQueryEnumerator<LavalandVisitorComponent>();
        while (query.MoveNext(out var uid, out var visitor))
        {
            if (visitor.ImmuneToStorm)
                continue;

            var transform = Transform(uid);
            if (transform.ParentUid != lavalandUid)
                continue;

            // Activity don't care about you or who you are.
            if (comp.CurrentWeatherType != LavalandWeatherType.VolcanicActivity)
            {
                if (!_turf.TryGetTileRef(transform.Coordinates, out var tileRef))
                    continue;

                var tile = _turf.GetContentTileDefinition(tileRef.Value);
                if (!tile.Weather)
                    continue;
            }

            var damage = GetWeatherDamage(comp.CurrentWeatherType);
            if (damage != null)
            {
                _damage.TryChangeDamage(uid, damage);
            }

            ApplyWeatherEffects(uid, comp.CurrentWeatherType);
        }
    }

    private DamageSpecifier? GetWeatherDamage(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => new DamageSpecifier { DamageDict = { { HeatDamage, 10 } } },
            LavalandWeatherType.AshStormHeavy => new DamageSpecifier { DamageDict = { { HeatDamage, 40 } } },
            LavalandWeatherType.AcidRain => new DamageSpecifier { DamageDict = { { CausticDamage, 10 } } },
            _ => null
        };
    }

    // More Cinema
    private void ApplyWeatherEffects(EntityUid targetUid, LavalandWeatherType type)
    {
        switch (type)
        {
            case LavalandWeatherType.StormWind:
                ApplyWindPush(targetUid);
                break;

            case LavalandWeatherType.VolcanicActivity:
                ApplyVolcanicActivity(targetUid);
                break;
        }
    }

    private void ApplyWindPush(EntityUid targetUid)
    {
        var windDirection = _random.NextAngle().ToVec();
        var windForce = _random.NextFloat(50f, 150f);

        _physics.ApplyLinearImpulse(targetUid, windDirection * windForce);
    }

    #region Volcanic Activity

    private void ApplyVolcanicActivity(EntityUid? targetUid = null)
    {
        if (targetUid.HasValue)
        {
            ApplyEarthquakeToPlayer(targetUid.Value);
            if (_random.Prob(0.2f))
            {
                SpawnEffectsNearPlayer(targetUid.Value);
            }
        }
        else
        {
            var lavalandQuery = EntityQueryEnumerator<LavalandComponent>();
            while (lavalandQuery.MoveNext(out var lavalandUid, out _))
            {
                var mapUid = Transform(lavalandUid).MapUid;
                if (mapUid == null)
                    continue;

                var min = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMin);
                var max = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMax);

                int attempts = 0;
                int maxAttempts = 10;
                int spawnedCount = 0;
                int maxSpawns = _random.Next(2, 5);

                while (spawnedCount < maxSpawns && attempts < maxAttempts)
                {
                    attempts++;

                    var angle = _random.NextAngle();
                    var distance = _random.NextFloat(min, max);
                    var spawnPos = angle.ToVec() * distance;

                    var spawnCoords = new EntityCoordinates(mapUid.Value, spawnPos);
                    if (_lookup.GetEntitiesInRange<ActorComponent>(spawnCoords, 1f).ToList().Count > 0)
                        continue;

                    var effectRoll = _random.Next(100);

                    if (effectRoll < 70)
                    {
                        SpawnRockFormation(mapUid.Value, spawnPos);
                        spawnedCount++;
                    }
                    else
                    {
                        SpawnLavaFormation(mapUid.Value, spawnPos);
                        spawnedCount++;
                    }
                }
            }
        }
    }

    #region Player Effects

    private void ApplyEarthquakeToPlayer(EntityUid playerUid)
    {
        if (!TryComp<CameraRecoilComponent>(playerUid, out var recoil))
            return;

        var intensity = _random.NextFloat(0.3f, 0.7f);
        ApplyCameraShake(playerUid, intensity, recoil);

        if (_random.Prob(0.3f))
        {
            ApplyStrongShake(playerUid, recoil);
        }
    }

    private void ApplyCameraShake(EntityUid playerUid, float intensity, CameraRecoilComponent recoil)
    {
        var direction = _random.NextAngle().ToVec();
        var shakeMagnitude = intensity * 0.5f;

        var kickback = direction * shakeMagnitude;
        _recoil.KickCamera(playerUid, kickback, recoil);
        if (_random.Prob(intensity * 0.5f))
            TryKnockDown(playerUid);
    }

    private void ApplyStrongShake(EntityUid playerUid, CameraRecoilComponent recoil)
    {
        var direction = _random.NextAngle().ToVec();
        var strongKick = direction * _random.NextFloat(0.4f, 0.8f);

        Timer.Spawn(TimeSpan.FromSeconds(_random.NextFloat(0.1f, 0.5f)),
            () => _recoil.KickCamera(playerUid, strongKick, recoil));
    }

    private void TryKnockDown(EntityUid playerUid)
    {
        var knockDirection = _random.NextAngle().ToVec();
        var knockForce = _random.NextFloat(50f, 150f);

        _physics.ApplyLinearImpulse(playerUid, knockDirection * knockForce);

        if (_random.Prob(0.2f))
        {
            var time = TimeSpan.FromSeconds(_random.Next(1, 3));
            _stun.TryKnockdown(playerUid, time);
        }
    }

    private void SpawnEffectsNearPlayer(EntityUid playerUid)
    {
        var playerPos = _transform.GetWorldPosition(playerUid);
        var mapUid = Transform(playerUid).MapUid;

        if (mapUid == null)
            return;

        var direction = _random.NextAngle().ToVec();

        var effectRoll = _random.Next(100);

        if (effectRoll < 70)
        {
            var safeDistance = 6f;
            var formationPos = playerPos + direction * safeDistance;
            SpawnRockFormation(mapUid.Value, formationPos);
        }
        else
        {
            var safeDistance = 8f;
            var formationPos = playerPos + direction * safeDistance;
            SpawnLavaFormation(mapUid.Value, formationPos);
        }
    }
    #endregion

    #region Formations
    private void SpawnRockFormation(EntityUid mapUid, Vector2 centerPos)
    {
        var size = _random.Next(0, 2) == 0 ? 3 : 5;
        for (int x = -size / 2; x <= size / 2; x++)
        {
            for (int y = -size / 2; y <= size / 2; y++)
            {
                if (_random.Prob(0.8f))
                {
                    var spawnPos = centerPos + new Vector2(x, y);
                    var spawnCoords = new EntityCoordinates(mapUid, spawnPos);

                    var swampeds = _lookup.GetEntitiesInRange<DamageableComponent>(spawnCoords, 1f);
                    foreach (var swamped in swampeds)
                    {
                        var damage = new DamageSpecifier { DamageDict = { { BluntDamage, 200 }, { StructuralDamage, 200 } } };
                        _damage.TryChangeDamage(swamped.Owner, damage);
                    }

                    var rockProto = GetRandomRockPrototype();
                    Spawn(rockProto, spawnCoords);
                }
            }
        }
    }

    private void SpawnLavaFormation(EntityUid mapUid, Vector2 centerPos)
    {
        var size = GetLavaFormationSize();
        for (int x = -size / 2; x <= size / 2; x++)
        {
            for (int y = -size / 2; y <= size / 2; y++)
            {
                var spawnChance = GetLavaSpawnChance(x, y, size);

                if (_random.Prob(spawnChance))
                {
                    var spawnPos = centerPos + new Vector2(x, y);
                    var spawnCoords = new EntityCoordinates(mapUid, spawnPos);

                    Spawn("FloorLavaEntity", spawnCoords);
                }
            }
        }
    }

    private string GetRandomRockPrototype()
    {
        var rockWeights = new (string Prototype, int Weight)[]
        {
            ("WallRockBasalt", 50),
            ("WallRockBasaltLavalandTin", 20),
            ("WallRockBasaltLavalandCoal", 15),
            ("WallRockBasaltLavalandPlasma", 8),
            ("WallRockBasaltLavalandSilver", 4),
            ("WallRockBasaltLavalandGold", 2),
            ("WallRockBasaltLavalandUranium", 1),
        };

        var totalWeight = 0;
        foreach (var (_, weight) in rockWeights)
        {
            totalWeight += weight;
        }

        var roll = _random.Next(totalWeight);
        var currentWeight = 0;

        foreach (var (prototype, weight) in rockWeights)
        {
            currentWeight += weight;
            if (roll < currentWeight)
                return prototype;
        }

        return "WallRockBasalt";
    }

    private int GetLavaFormationSize()
    {
        var roll = _random.Next(100);

        return roll switch
        {
            < 50 => 3,
            < 80 => 5,
            < 95 => 7,
            _ => 9
        };
    }

    private float GetLavaSpawnChance(int x, int y, int size)
    {
        var distanceFromCenter = Math.Sqrt(x * x + y * y);
        var maxDistance = size / 2f;

        var centerChance = 0.9f;
        var edgeChance = 0.6f;

        var normalizedDistance = (float)distanceFromCenter / maxDistance;
        return MathHelper.Lerp(centerChance, edgeChance, normalizedDistance);
    }

    #endregion
    #endregion

    private void SendWeatherAlert(LavalandWeatherType weatherType)
    {
        Entity<LavalandAvanpostComponent>? sender = null;
        var query = EntityQueryEnumerator<LavalandAvanpostComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            sender = (uid, comp);
            break;
        }

        if (sender == null)
            return;

        var alertMessage = GetWeatherWarningMessage(weatherType);
        _radio.SendRadioMessage(sender.Value.Owner, alertMessage, sender.Value.Comp.AnnouncementChannel,
            sender.Value.Owner, escapeMarkup: false);
    }

    private void SendWeatherEndAlert(LavalandWeatherType weatherType)
    {
        Entity<LavalandAvanpostComponent>? sender = null;
        var query = EntityQueryEnumerator<LavalandAvanpostComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            sender = (uid, comp);
            break;
        }

        if (sender == null)
            return;

        var alertMessage = GetWeatherEndMessage(weatherType);
        _radio.SendRadioMessage(sender.Value.Owner, alertMessage, sender.Value.Comp.AnnouncementChannel,
            sender.Value.Owner, escapeMarkup: false);
    }

    private string GetWeatherWarningMessage(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => Loc.GetString("lavaland-weather-warning-ash-storm-light"),
            LavalandWeatherType.AshStormHeavy => Loc.GetString("lavaland-weather-warning-ash-storm-heavy"),
            LavalandWeatherType.VolcanicActivity => Loc.GetString("lavaland-weather-warning-volcanic-activity"),
            LavalandWeatherType.AcidRain => Loc.GetString("lavaland-weather-warning-acid-rain"),
            LavalandWeatherType.StormWind => Loc.GetString("lavaland-weather-warning-wind"),
            _ => Loc.GetString("lavaland-weather-warning-default")
        };
    }

    private string GetWeatherEndMessage(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => Loc.GetString("lavaland-weather-end-ash-storm-light"),
            LavalandWeatherType.AshStormHeavy => Loc.GetString("lavaland-weather-end-ash-storm-heavy"),
            LavalandWeatherType.VolcanicActivity => Loc.GetString("lavaland-weather-end-volcanic-activity"),
            LavalandWeatherType.AcidRain => Loc.GetString("lavaland-weather-end-acid-rain"),
            LavalandWeatherType.StormWind => Loc.GetString("lavaland-weather-end-wind"),
            _ => Loc.GetString("lavaland-weather-end-default")
        };
    }

    #endregion

    #region Lavaland Procesing

    private void OnStationStartup(Entity<StationLavalandComponent> ent, ref StationPostInitEvent args)
    {
        if (!_cfg.GetCVar(WegaCVars.LavalandEnabled))
        {
            Log.Info("Lavaland processing is currently disabled.");
            return;
        }

        AddLavaland(ent);
    }

    private void AddLavaland(Entity<StationLavalandComponent> ent)
    {
        var mapUid = _map.CreateMap(out var mapId);
        if (!_loader.TryLoadGrid(mapId, ent.Comp.LavalandAvanpostPath, out var avanpost, offset: Vector2.Zero))
        {
            Log.Error($"Unable to load lavaland avanpost map {ent.Comp.LavalandAvanpostPath} for {ToPrettyString(ent)}");
            _map.DeleteMap(mapId);
            return;
        }

        _meta.SetEntityName(mapUid, Loc.GetString("lavaland-map"));

        var avanpostComp = EnsureComp<LavalandAvanpostComponent>(avanpost.Value);
        EnsureComp<ActiveRadioComponent>(avanpost.Value).Channels.Add(avanpostComp.AnnouncementChannel);
        EnsureComp<ProtectedGridComponent>(avanpost.Value);
        EnsureComp<ProtectedGridComponent>(mapUid);
        EnsureComp<NavMapComponent>(mapUid);

        _biome.EnsurePlanet(mapUid, _proto.Index(ent.Comp.Biome), ent.Comp.Seed, mapLight: ent.Comp.MapLightColor);
        EnsureComp<LavalandComponent>(mapUid).NextWeatherChange = _gameTiming.CurTime + TimeSpan.FromMinutes(_random.Next(5, 15));

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int)Gas.Oxygen] = 14.022f;
        moles[(int)Gas.Nitrogen] = 22.878f;

        var mixture = new GasMixture(moles, 299.15f);
        _atmos.SetMapAtmosphere(mapUid, false, mixture);

        GenerateBuildings(mapId, mapUid);
    }

    public void GenerateBuildings(MapId mapId, EntityUid mainGrid)
    {
        var buildingList = _proto.EnumeratePrototypes<LavalandBuildingPrototype>()
            .OrderByDescending(b => b.ExactPosition.HasValue)
            .ThenBy(b => _random.Next()).ToList();

        for (var i = 0; i <= _cfg.GetCVar(WegaCVars.LavalandMaxBuildings) && i < buildingList.Count; i++)
            SpawnBuilding(mapId, buildingList[i], mainGrid);
    }

    private void SpawnBuilding(MapId mapId, LavalandBuildingPrototype proto, EntityUid mainGrid)
    {
        Vector2 position;
        if (proto.ExactPosition.HasValue)
        {
            position = proto.ExactPosition.Value;
        }
        else
        {
            var min = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMin);
            var max = _cfg.GetCVar(WegaCVars.LavalandSpawnIntervalMax);

            var angle = _random.NextAngle();
            var distance = _random.NextFloat(min, max);
            position = angle.ToVec() * distance;
        }

        var alignedPosition = new Vector2((int)position.X, (int)position.Y);
        if (!_loader.TryLoadGrid(mapId, proto.GridPath, out var buildingGrid, offset: alignedPosition))
        {
            Log.Error($"Failed to load lavaland building {proto.ID} at {position}");
            return;
        }

        _meta.SetEntityName(buildingGrid.Value, "");
        if (proto.MergeWithPlanet && mainGrid != buildingGrid.Value.Owner)
        {
            MergeWithPlanet(mainGrid, buildingGrid.Value.Owner, alignedPosition);
        }
        else
        {
            _iff.AddIFFFlag(buildingGrid.Value, IFFFlags.Hide);
            EnsureComp<ProtectedGridComponent>(buildingGrid.Value);
        }
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

            var ents = new HashSet<Entity<TransformComponent, MetaDataComponent>>();
            _lookup.GetChildEntities(buildingGridUid, ents);
            foreach (var ent in ents)
            {
                if (ent.Owner == buildingGridUid)
                    continue;

                _transform.SetCoordinates(ent, new EntityCoordinates(mainGridUid, ent.Comp1.LocalPosition + offsetPosition), ent.Comp1.LocalRotation, false);
            }

            _fixture.Merge(mainGridUid, buildingGridUid, offset, Angle.Zero, mainGrid, buildingGrid);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to merge grids: {ex.Message}");
        }
    }

    #endregion
}
