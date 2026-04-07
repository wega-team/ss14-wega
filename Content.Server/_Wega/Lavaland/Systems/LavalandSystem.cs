using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Server.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Shared.Atmos;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Decals;
using Content.Shared.Gravity;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Tiles;
using Content.Shared.Weather;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

// TODO: До лучших времен доделать настройки планеты через прото под [TOP SECRET].
public sealed partial class LavalandSystem : SharedLavalandSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GridFixtureSystem _fixture = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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
    private static readonly EntProtoId FallingRock = "FallingRockEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationLavalandComponent, StationPostInitEvent>(OnStationStartup, before: [typeof(ConditionalSpawnerSystem)]);
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
        var weatherType = GetRandomWeatherType(comp);
        var (weatherProto, _) = GetWeatherInfo(weatherType);

        comp.UpcomingWeatherType = weatherType;
        comp.UpcomingWeatherProto = weatherProto;
        comp.WeatherStartTime = _gameTiming.CurTime + TimeSpan.FromSeconds(60);
        comp.WarningSent = true;

        SendWeatherAlert(weatherType);
    }

    private void StartWeather(EntityUid uid, LavalandComponent comp)
    {
        var mapId = Transform(uid).MapID;

        comp.CurrentWeatherType = comp.UpcomingWeatherType;
        comp.CurrentWeatherProto = comp.UpcomingWeatherProto;
        comp.CurrentWeatherEnd = _gameTiming.CurTime + GetWeatherInfo(comp.CurrentWeatherType).duration;

        comp.UpcomingWeatherType = LavalandWeatherType.None;
        comp.UpcomingWeatherProto = null;
        comp.WarningSent = false;

        comp.NextWeatherChange = comp.CurrentWeatherEnd + TimeSpan.FromMinutes(_random.Next(5, 15));

        if (comp.CurrentWeatherProto != null)
        {
            _weather.TrySetWeather(mapId, comp.CurrentWeatherProto.Value, out _, comp.CurrentWeatherEnd - _gameTiming.CurTime);
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
                ApplyVolcanicActivity(comp);
        }
    }

    private void EndWeather(EntityUid uid, LavalandComponent comp)
    {
        var endedWeather = comp.CurrentWeatherType;
        var mapId = Transform(uid).MapID;

        comp.CurrentWeatherType = LavalandWeatherType.None;
        comp.CurrentWeatherProto = null;
        comp.DamageTick = 0f;

        _weather.TrySetWeather(mapId, null, out _);

        SendWeatherEndAlert(endedWeather);
    }

    private LavalandWeatherType GetRandomWeatherType(LavalandComponent comp)
    {
        if (!_proto.TryIndex(comp.PlanetPrototype, out LavalandPlanetPrototype? planetProto))
            return LavalandWeatherType.None;

        var availableWeather = planetProto.AvailableWeather;
        if (availableWeather.Count == 0)
            return LavalandWeatherType.None;

        var weatherWeights = new Dictionary<LavalandWeatherType, int>
        {
            { LavalandWeatherType.StormWind, 40 },
            { LavalandWeatherType.AshStormLight, 30 },
            { LavalandWeatherType.AshStormHeavy, 15 },
            { LavalandWeatherType.VolcanicActivity, 10 },
            { LavalandWeatherType.AcidRain, 5 }
        };

        var filteredWeights = weatherWeights
            .Where(x => availableWeather.Contains(x.Key))
            .ToList();

        if (filteredWeights.Count == 0)
            return LavalandWeatherType.None;

        var totalWeight = filteredWeights.Sum(x => x.Value);
        var roll = _random.Next(totalWeight);
        var currentWeight = 0;

        foreach (var (weather, weight) in filteredWeights)
        {
            currentWeight += weight;
            if (roll < currentWeight)
                return weather;
        }

        return filteredWeights.First().Key;
    }

    private (EntProtoId? proto, TimeSpan duration) GetWeatherInfo(LavalandWeatherType type)
    {
        // Yeeeeeeeeeeee, the fierce hardcore proved itself in one upstream, after which a refactor was planned!
        return type switch
        {
            LavalandWeatherType.AshStormLight => ("WeatherAshfallLight", TimeSpan.FromSeconds(_random.Next(60, 120))),
            LavalandWeatherType.AshStormHeavy => ("WeatherAshfallHeavy", TimeSpan.FromSeconds(_random.Next(90, 150))),
            LavalandWeatherType.VolcanicActivity => (null, TimeSpan.FromSeconds(_random.Next(60, 120))),
            LavalandWeatherType.AcidRain => ("WeatherAcidRain", TimeSpan.FromSeconds(_random.Next(60, 120))),
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
            if (transform.MapUid != lavalandUid)
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
                var ev = new AshProtectionAttemptEvent();
                RaiseLocalEvent(uid, ref ev);

                if (ev.Modifier < 1f)
                {
                    var damageReduction = 1f - ev.Modifier;
                    _damage.TryChangeDamage(uid, damage * damageReduction, true);
                    _popup.PopupEntity(Loc.GetString(GetWeatherDamageMessage(comp.CurrentWeatherType)),
                        uid, uid);
                }
            }

            ApplyWeatherEffects(uid, comp);
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
    private void ApplyWeatherEffects(EntityUid targetUid, LavalandComponent comp)
    {
        switch (comp.CurrentWeatherType)
        {
            case LavalandWeatherType.StormWind:
                ApplyWindPush(targetUid);
                break;

            case LavalandWeatherType.VolcanicActivity:
                ApplyVolcanicActivity(comp, targetUid);
                break;
        }
    }

    private void ApplyWindPush(EntityUid targetUid)
    {
        var windDirection = _random.NextAngle().ToVec();
        var windForce = _random.NextFloat(500f, 1500f);

        if (HasComp<PhysicsComponent>(targetUid))
        {
            _physics.ApplyLinearImpulse(targetUid, windDirection * windForce);
        }
    }

    #region Volcanic Activity

    private void ApplyVolcanicActivity(LavalandComponent comp, EntityUid? targetUid = null)
    {
        if (targetUid.HasValue)
        {
            ApplyEarthquakeToPlayer(targetUid.Value, comp.RumbleSound);
            if (_random.Prob(0.1f))
            {
                SpawnEffectsNearPlayer(targetUid.Value, comp.RockFallSound);
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
                int maxAttempts = 3;
                int spawnedCount = 0;
                int maxSpawns = _random.Next(1, 3);

                while (spawnedCount < maxSpawns && attempts < maxAttempts)
                {
                    attempts++;

                    var angle = _random.NextAngle();
                    var distance = _random.NextFloat(min, max);
                    var spawnPos = angle.ToVec() * distance;

                    var spawnCoords = new EntityCoordinates(mapUid.Value, spawnPos);
                    if (_lookup.GetEntitiesInRange<ActorComponent>(spawnCoords, 1f).Any())
                        continue;

                    var protectedGrids = _lookup.GetEntitiesInRange<GridLavalandWeatherProtectionComponent>(spawnCoords, 10f);
                    if (protectedGrids.Any())
                        continue;

                    var avanpost = _lookup.GetEntitiesInRange<LavalandAvanpostComponent>(spawnCoords, 16f);
                    if (avanpost.Any())
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

    private void ApplyEarthquakeToPlayer(EntityUid playerUid, SoundSpecifier sound)
    {
        if (!TryComp<CameraRecoilComponent>(playerUid, out var recoil))
            return;

        var intensity = _random.NextFloat(0.3f, 0.7f);
        ApplyCameraShake(playerUid, intensity, recoil);
        _audio.PlayEntity(sound, playerUid, playerUid);

        if (_random.Prob(0.3f))
        {
            ApplyStrongShake(playerUid, sound, recoil);
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

    private void ApplyStrongShake(EntityUid playerUid, SoundSpecifier sound, CameraRecoilComponent recoil)
    {
        var direction = _random.NextAngle().ToVec();
        var strongKick = direction * _random.NextFloat(0.4f, 0.8f);

        Timer.Spawn(TimeSpan.FromSeconds(_random.NextFloat(0.1f, 0.5f)),
            () =>
            {
                _recoil.KickCamera(playerUid, strongKick, recoil);
                _audio.PlayEntity(sound, playerUid, playerUid);
            });
    }

    private void TryKnockDown(EntityUid playerUid)
    {
        var knockDirection = _random.NextAngle().ToVec();
        var knockForce = _random.NextFloat(500f, 1500f);

        if (HasComp<PhysicsComponent>(playerUid))
        {
            _physics.ApplyLinearImpulse(playerUid, knockDirection * knockForce);
        }

        if (_random.Prob(0.2f))
        {
            var time = TimeSpan.FromSeconds(_random.Next(1, 3));
            _stun.TryKnockdown(playerUid, time);
        }
    }

    private void SpawnEffectsNearPlayer(EntityUid playerUid, SoundSpecifier sound)
    {
        var playerPos = _transform.GetWorldPosition(playerUid);
        var mapUid = Transform(playerUid).MapUid;

        if (mapUid == null)
            return;

        var avanpost = _lookup.GetEntitiesInRange<LavalandAvanpostComponent>(Transform(playerUid).Coordinates, 48f);
        if (avanpost.Count > 0)
            return;

        var gridnWeatherProtection = _lookup.GetEntitiesInRange<GridLavalandWeatherProtectionComponent>(Transform(playerUid).Coordinates, 48f);
        if (gridnWeatherProtection.Count > 0)
            return;

        var direction = _random.NextAngle().ToVec();
        var effectRoll = _random.Next(100);

        if (effectRoll < 80)
        {
            var safeDistance = 6f;
            var formationPos = playerPos + direction * safeDistance;

            Spawn(FallingRock, new EntityCoordinates(mapUid.Value, formationPos));
            Timer.Spawn(TimeSpan.FromSeconds(5f),
            () =>
            {
                SpawnRockFormation(mapUid.Value, formationPos);
                _audio.PlayPredicted(sound, new EntityCoordinates(mapUid.Value, formationPos), null);
            });
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

    private string GetWeatherDamageMessage(LavalandWeatherType type)
    {
        return type switch
        {
            LavalandWeatherType.AshStormLight => Loc.GetString("lavaland-weather-damaged-ash-storm-light"),
            LavalandWeatherType.AshStormHeavy => Loc.GetString("lavaland-weather-damaged-ash-storm-heavy"),
            LavalandWeatherType.AcidRain => Loc.GetString("lavaland-weather-damaged-acid-rain"),
            _ => Loc.GetString("lavaland-weather-damaged-default")
        };
    }

    #endregion

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

        _meta.SetEntityName(mapUid, Loc.GetString("lavaland-map"));
        _meta.SetEntityName(avanpost.Value, Loc.GetString("lavaland-map-avanpost"));
        var avanpostComp = EnsureComp<LavalandAvanpostComponent>(avanpost.Value);
        EnsureComp<ActiveRadioComponent>(avanpost.Value).Channels.Add(avanpostComp.AnnouncementChannel);
        EnsureComp<ProtectedGridComponent>(avanpost.Value);
        EnsureComp<ProtectedGridComponent>(mapUid);

        var grid = EnsureComp<MapGridComponent>(mapUid); // For build processing after creating planet
        EnsureComp<NavMapComponent>(mapUid);

        _map.CreateMap(out var tempMapId);

        var worldAABBs = new HashSet<Box2>();
        GenerateBuildings(mapId, tempMapId, mapUid, ref worldAABBs);

        _biome.EnsurePlanet(mapUid, _proto.Index(planet.Biome), ent.Comp.Seed, mapLight: planet.MapLightColor);
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

    public void GenerateBuildings(MapId mapId, MapId tempMapId, EntityUid mainGrid, ref HashSet<Box2> worldAABBs)
    {
        var buildings = _proto.EnumeratePrototypes<LavalandBuildingPrototype>();
        var buildingList = buildings.Select(b => new { Building = b, RandomValue = _random.Next() })
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
            EnsureComp<NavMapComponent>(buildingGrid.Value);

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
