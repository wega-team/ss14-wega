using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weather;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private TurfSystem _turf = default!;

    private static readonly ProtoId<DamageTypePrototype> StructuralDamage = "Structural";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly EntProtoId FallingRock = "FallingRockEffect";

    #region Weather Processing

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LavalandComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextWeatherChange < _gameTiming.CurTime && !comp.WarningSent)
                SendWeatherWarning(comp);

            if (comp.WeatherStartTime < _gameTiming.CurTime && comp.WarningSent && comp.CurrentWeatherEntry == null)
                StartWeather(uid, comp);

            if (comp.CurrentWeatherEntry != null)
                ProcessWeather(uid, comp, frameTime);
        }
    }

    private void SendWeatherWarning(LavalandComponent comp)
    {
        var weatherEntry = GetRandomWeatherEntry(comp);
        if (weatherEntry == null)
            return;

        comp.UpcomingWeatherEntry = weatherEntry;
        comp.WeatherStartTime = _gameTiming.CurTime + TimeSpan.FromSeconds(60);
        comp.WarningSent = true;

        SendWeatherAlert(weatherEntry);
    }

    private void StartWeather(EntityUid uid, LavalandComponent comp)
    {
        if (comp.UpcomingWeatherEntry == null)
        {
            Log.Error($"Trying to start weather with null UpcomingWeatherEntry on {ToPrettyString(uid)}");

            comp.WarningSent = false;
            comp.NextWeatherChange = _gameTiming.CurTime + TimeSpan.FromMinutes(_random.Next(5, 15));
            return;
        }

        var mapId = Transform(uid).MapID;

        comp.CurrentWeatherEntry = comp.UpcomingWeatherEntry;
        comp.CurrentWeatherEnd = _gameTiming.CurTime + GetWeatherInfo(comp.CurrentWeatherEntry).duration;

        comp.UpcomingWeatherEntry = null;
        comp.WarningSent = false;

        comp.NextWeatherChange = comp.CurrentWeatherEnd + TimeSpan.FromMinutes(_random.Next(5, 15));

        if (comp.CurrentWeatherEntry?.WeatherPrototype != null)
        {
            _weather.TryAddWeather(mapId, comp.CurrentWeatherEntry.WeatherPrototype.Value, out _,
                comp.CurrentWeatherEnd - _gameTiming.CurTime);
        }
    }

    private void ProcessWeather(EntityUid uid, LavalandComponent comp, float frameTime)
    {
        if (comp.CurrentWeatherEntry == null)
        {
            EndWeather(uid, comp);
            return;
        }

        if (comp.CurrentWeatherEnd < _gameTiming.CurTime)
        {
            EndWeather(uid, comp);
            return;
        }

        comp.DamageTick -= frameTime;
        if (comp.DamageTick <= 0f)
        {
            ApplyWeatherDamage(uid, comp);
            comp.DamageTick = comp.CurrentWeatherEntry.DamageIntervalSeconds;

            if (comp.CurrentWeatherEntry?.SpecialEffect == LavalandWeatherType.VolcanicActivity)
                ApplyVolcanicActivity(comp);
        }
    }

    private void EndWeather(EntityUid uid, LavalandComponent comp)
    {
        var endedWeather = comp.CurrentWeatherEntry;
        var mapId = Transform(uid).MapID;

        comp.CurrentWeatherEntry = null;
        comp.DamageTick = 0f;

        if (endedWeather != null && endedWeather.WeatherPrototype != null)
        {
            _weather.TryRemoveWeather(mapId, endedWeather.WeatherPrototype.Value);
        }

        if (endedWeather != null)
            SendWeatherEndAlert(endedWeather);
    }

    private LavalandWeatherEntryPrototype? GetRandomWeatherEntry(LavalandComponent comp)
    {
        if (!_proto.TryIndex(comp.PlanetPrototype, out LavalandPlanetPrototype? planetProto))
            return null;

        var availableWeather = planetProto.AvailableWeather;
        if (availableWeather.Count == 0)
            return null;

        var weatherEntries = new List<LavalandWeatherEntryPrototype>();
        var totalWeight = 0;

        foreach (var weatherId in availableWeather)
        {
            if (!_proto.TryIndex(weatherId, out LavalandWeatherEntryPrototype? weatherEntry))
                continue;

            weatherEntries.Add(weatherEntry);
            totalWeight += weatherEntry.Weight;
        }

        if (weatherEntries.Count == 0 || totalWeight == 0)
            return null;

        var roll = _random.Next(totalWeight);
        var currentWeight = 0;

        foreach (var weatherEntry in weatherEntries)
        {
            currentWeight += weatherEntry.Weight;
            if (roll < currentWeight)
                return weatherEntry;
        }

        return weatherEntries.First();
    }

    private (EntProtoId? proto, TimeSpan duration) GetWeatherInfo(LavalandWeatherEntryPrototype weatherEntry)
    {
        var duration = TimeSpan.FromSeconds(_random.NextFloat(weatherEntry.MinDurationSeconds, weatherEntry.MaxDurationSeconds));
        return (weatherEntry.WeatherPrototype, duration);
    }

    private void ApplyWeatherDamage(EntityUid lavalandUid, LavalandComponent comp)
    {
        if (comp.CurrentWeatherEntry == null)
            return;

        var query = EntityQueryEnumerator<LavalandVisitorComponent>();
        while (query.MoveNext(out var uid, out var visitor))
        {
            if (visitor.ImmuneToStorm)
                continue;

            var transform = Transform(uid);
            if (transform.MapUid != lavalandUid)
                continue;

            var hasSpecialEffect = comp.CurrentWeatherEntry.SpecialEffect != null;
            if (!hasSpecialEffect)
            {
                if (!_turf.TryGetTileRef(transform.Coordinates, out var tileRef))
                    continue;

                var tile = _turf.GetContentTileDefinition(tileRef.Value);
                if (!tile.Weather)
                    continue;
            }

            var damage = comp.CurrentWeatherEntry.Damage;
            if (damage != null)
            {
                var ev = new AshProtectionAttemptEvent();
                RaiseLocalEvent(uid, ref ev);

                if (ev.Modifier < 1f)
                {
                    var damageReduction = 1f - ev.Modifier;
                    _damage.TryChangeDamage(uid, damage * damageReduction, true);
                    _popup.PopupEntity(Loc.GetString(comp.CurrentWeatherEntry.DamageMessage),
                        uid, uid);
                }
            }

            ApplyWeatherEffects(uid, comp);
        }
    }

    private void ApplyWeatherEffects(EntityUid targetUid, LavalandComponent comp)
    {
        if (comp.CurrentWeatherEntry?.SpecialEffect == null)
            return;

        switch (comp.CurrentWeatherEntry.SpecialEffect.Value)
        {
            case LavalandWeatherType.StormWind:
                ApplyWindPush(targetUid);
                break;

            case LavalandWeatherType.VolcanicActivity:
                ApplyVolcanicActivity(comp, targetUid);
                break;
        }
    }

    private void SendWeatherAlert(LavalandWeatherEntryPrototype weatherEntry)
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

        _radio.SendRadioMessage(sender.Value.Owner, Loc.GetString(weatherEntry.WarningMessage),
            sender.Value.Comp.AnnouncementChannel, sender.Value.Owner, escapeMarkup: false);
    }

    private void SendWeatherEndAlert(LavalandWeatherEntryPrototype weatherEntry)
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

        _radio.SendRadioMessage(sender.Value.Owner, Loc.GetString(weatherEntry.EndMessage),
            sender.Value.Comp.AnnouncementChannel, sender.Value.Owner, escapeMarkup: false);
    }

    #endregion

    #region Wind Effect

    private void ApplyWindPush(EntityUid targetUid)
    {
        var windDirection = _random.NextAngle().ToVec();
        var windForce = _random.NextFloat(500f, 1500f);

        if (HasComp<PhysicsComponent>(targetUid))
        {
            _physics.ApplyLinearImpulse(targetUid, windDirection * windForce);
        }
    }

    #endregion

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

        var gridWeatherProtection = _lookup.GetEntitiesInRange<GridLavalandWeatherProtectionComponent>(Transform(playerUid).Coordinates, 48f);
        if (gridWeatherProtection.Count > 0)
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
}
