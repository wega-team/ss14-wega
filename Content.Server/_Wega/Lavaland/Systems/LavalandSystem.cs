using System.Numerics;
using Content.Server.Lavaland.Components;
using Content.Server.Parallax;
using Content.Server.Station.Events;
using Content.Shared.Damage;
using Content.Shared.Lavaland.Components;
using Content.Shared.Tiles;
using Content.Shared.Weather;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private const float MinDisatnce = 250f;
    private const float MaxDisatnce = 1500f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationLavalandComponent, StationPostInitEvent>(OnStationStartup);
        SubscribeLocalEvent<LavalandVisitorComponent, EntParentChangedMessage>(OnVisitorEntParentChanged);
    }

    #region AshStorm

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LavalandComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextStormTime < _gameTiming.CurTime)
            {
                StartStorm(uid, comp);
                comp.NextStormTime = _gameTiming.CurTime + TimeSpan.FromMinutes(_random.Next(10, 20));
            }

            if (comp.StormSeverity > 0)
            {
                UpdateStormSeverity(uid, comp);

                if (comp.CurrentStormProto == "AshfallHeavy")
                {
                    comp.DamageTick -= frameTime;
                    if (comp.DamageTick <= 0f)
                    {
                        ApplyStormDamage();
                        comp.DamageTick = 5f;
                    }
                }
            }
        }
    }

    private void StartStorm(EntityUid uid, LavalandComponent comp)
    {
        comp.StormSeverity = 0.01f;
        comp.CurrentStormProto = "AshfallLight";

        _weather.SetWeather(Transform(uid).MapID, _proto.Index(comp.CurrentStormProto), null);
    }

    private void UpdateStormSeverity(EntityUid uid, LavalandComponent comp)
    {
        comp.StormSeverity = Math.Min(comp.StormSeverity + 0.0001f, 1f); // ~5 minutes

        var newProto = comp.StormSeverity switch
        {
            >= 0.9f => "AshfallHeavy",
            >= 0.4f => "Ashfall",
            _ => "AshfallLight"
        };

        if (newProto != comp.CurrentStormProto)
        {
            comp.CurrentStormProto = newProto;
            _weather.SetWeather(Transform(uid).MapID, _proto.Index(comp.CurrentStormProto), null);
        }

        if (comp.StormSeverity >= 1f)
        {
            comp.StormSeverity = 0f;
            ProtoId<WeatherPrototype> stormEnd = "AshfallLight";
            _weather.SetWeather(Transform(uid).MapID, _proto.Index(stormEnd), _gameTiming.CurTime + TimeSpan.FromSeconds(60f));
        }
    }

    private void ApplyStormDamage()
    {
        var query = EntityQueryEnumerator<LavalandVisitorComponent>();
        while (query.MoveNext(out var uid, out var visitor))
        {
            if (!visitor.InLavaland || visitor.ImmuneToStorm)
                continue;

            var damage = new DamageSpecifier { DamageDict = { { "Heat", 40 } } };
            _damage.TryChangeDamage(uid, damage);
        }
    }

    #endregion

    #region Lavaland Procesing

    private void OnStationStartup(Entity<StationLavalandComponent> ent, ref StationPostInitEvent args)
    {
        AddLavaland(ent);
    }

    private void AddLavaland(Entity<StationLavalandComponent> ent)
    {
        var mapUid = _map.CreateMap(out var mapId);
        if (!_loader.TryLoadGrid(mapId, ent.Comp.LavalandAvanpostPath, out var avanpost))
        {
            Log.Error($"Unable to load lavaland avanpost map {ent.Comp.LavalandAvanpostPath} for {ToPrettyString(ent)}");
            _map.DeleteMap(mapId);
            return;
        }

        _meta.SetEntityName(mapUid, Loc.GetString("lavaland-map"));

        GenerateBuildings(mapId);
        EnsureComp<ProtectedGridComponent>(avanpost.Value.Owner);

        _biome.EnsureLavalandPlanet(mapUid, _proto.Index(ent.Comp.Biome), ent.Comp.Seed, mapLight: ent.Comp.MapLightColor);
    }

    public void GenerateBuildings(MapId mapId)
    {
        foreach (var proto in _proto.EnumeratePrototypes<LavalandBuildingPrototype>())
        {
            SpawnBuilding(mapId, proto);
        }
    }

    private void SpawnBuilding(MapId mapId, LavalandBuildingPrototype proto)
    {
        Vector2 position;
        if (proto.ExactPosition.HasValue)
        {
            position = proto.ExactPosition.Value;
        }
        else
        {
            var angle = _random.NextAngle();
            var distance = _random.NextFloat(MinDisatnce, MaxDisatnce);
            position = angle.ToVec() * distance;
        }

        if (!_loader.TryLoadGrid(mapId, proto.GridPath, out var grid, offset: position))
        {
            Log.Error($"Failed to load lavaland building {proto.ID} at {position}");
            return;
        }

        _meta.SetEntityName(grid.Value, "");
    }

    #endregion

    private void OnVisitorEntParentChanged(Entity<LavalandVisitorComponent> ent, ref EntParentChangedMessage args)
    {
        ent.Comp.InLavaland = HasComp<LavalandComponent>(Transform(ent).ParentUid);
    }
}