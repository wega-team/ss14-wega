using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandPenalServitudeShuttleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly DockingSystem _dockingSystem = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private static readonly string DockStation = "DockPenalServitudeStation";
    private static readonly string DockPenalServitude = "DockPenalServitude";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LavalandPenalServitudeShuttleComponent, MapInitEvent>(OnShuttleMapInit);
        SubscribeLocalEvent<LavalandPenalServitudeShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<StationLavalandPenalServitudeShuttleComponent, StationPostInitEvent>(OnStationStartup, after: [typeof(LavalandSystem)]);

        SubscribeLocalEvent<PenalServitudeShuttleConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<PenalServitudeShuttleConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PenalServitudeShuttleConsoleComponent, PenalServitudeLavalandShuttleCallMessage>(OnShuttleCall);
    }

    private void OnShuttleMapInit(EntityUid uid, LavalandPenalServitudeShuttleComponent component, MapInitEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<PenalServitudeShuttleConsoleComponent>();
        while (consoleQuery.MoveNext(out var console, out var consoleComp))
        {
            consoleComp.ConnectedShuttle = uid;
            UpdateLocation(console, consoleComp);
        }
    }

    private void OnShuttleArrival(EntityUid uid, LavalandPenalServitudeShuttleComponent component, ref FTLCompletedEvent args)
    {
        var shuttleDocks = _dockingSystem.GetDocks(uid);
        foreach (var (dockUid, dockComp) in shuttleDocks)
        {
            if (dockComp.DockedWith == null)
                continue;

            if (TryComp<PriorityDockComponent>(dockComp.DockedWith, out var oppositeDock))
            {
                component.State = oppositeDock.Tag switch
                {
                    _ when oppositeDock.Tag == DockStation => PenalServitudeLavalandShuttleState.DockedAtStation,
                    _ when oppositeDock.Tag == DockPenalServitude => PenalServitudeLavalandShuttleState.DockedAtPenalServitude,
                    _ => component.State
                };

                UpdateConsoles();
                return;
            }
        }

        var mapUid = Transform(uid).MapUid;
        if (mapUid != null)
        {
            component.State = HasComp<LavalandComponent>(mapUid)
                ? PenalServitudeLavalandShuttleState.DockedAtPenalServitude
                : PenalServitudeLavalandShuttleState.DockedAtStation;

            UpdateConsoles();
            return;
        }

        Log.Warning($"PenalServitude shuttle {ToPrettyString(uid)} arrived but no valid dock found!");
    }

    private void OnStationStartup(Entity<StationLavalandPenalServitudeShuttleComponent> ent, ref StationPostInitEvent args)
    {
        if (!_cfg.GetCVar(WegaCVars.LavalandEnabled))
            return;

        Timer.Spawn(100, () => AddPenalServitudeShuttle(ent));
    }

    private void AddPenalServitudeShuttle(Entity<StationLavalandPenalServitudeShuttleComponent> ent)
    {
        if (ent.Comp.PenalServitudeShuttle != null)
        {
            if (Exists(ent.Comp.PenalServitudeShuttle))
            {
                Log.Error($"Attempted to add a penal servitude shuttle to {ToPrettyString(ent)}, despite a shuttle already existing?");
                return;
            }
            Log.Error($"Encountered deleted penal servitude shuttle during initialization of {ToPrettyString(ent)}");
            ent.Comp.PenalServitudeShuttle = null;
        }

        _map.CreateMap(out var tempMapId);

        if (!_loader.TryLoadGrid(tempMapId, ent.Comp.PenalServitudeShuttlePath, out var shuttle))
        {
            Log.Error($"Unable to spawn penal servitude shuttle {ent.Comp.PenalServitudeShuttlePath} for {ToPrettyString(ent)}");
            _map.DeleteMap(tempMapId);
            return;
        }

        ent.Comp.PenalServitudeShuttle = shuttle.Value;
        EnsureComp<LavalandPenalServitudeShuttleComponent>(shuttle.Value);
        EnsureComp<ProtectedGridComponent>(shuttle.Value);

        bool docked = TryDockShuttle(shuttle.Value, DockStation, "station");

        if (!docked)
        {
            Log.Error($"Failed to dock penal servitude shuttle {ToPrettyString(shuttle)} to any available dock");
            _map.DeleteMap(tempMapId);
            ent.Comp.PenalServitudeShuttle = null;
            return;
        }

        _map.DeleteMap(tempMapId);
        Log.Info($"Added penal servitude shuttle {ToPrettyString(shuttle)} for station {ToPrettyString(ent)}");
    }

    private bool TryDockShuttle(EntityUid shuttle, string dockTag, string dockName)
    {
        var stationDock = FindDockWithTag(dockTag);
        if (stationDock == null)
        {
            Log.Warning($"Failed to find {dockName} dock with tag {dockTag} for penal servitude shuttle {ToPrettyString(shuttle)}");
            return false;
        }

        var stationGrid = Transform(stationDock.Value).GridUid;
        if (stationGrid == null)
        {
            Log.Error($"Penal servitude shuttle {ToPrettyString(shuttle)}: {dockName} dock {ToPrettyString(stationDock)} has no grid");
            return false;
        }

        var config = _dockingSystem.GetDockingConfig(shuttle, stationGrid.Value, dockTag);
        if (config == null)
        {
            Log.Warning($"Failed to find docking config for penal servitude shuttle {ToPrettyString(shuttle)} at {dockName} dock {ToPrettyString(stationDock)}");
            return false;
        }

        var shuttleXform = Transform(shuttle);
        _shuttleSystem.FTLDock((shuttle, shuttleXform), config);
        Log.Info($"Penal servitude shuttle {ToPrettyString(shuttle)} docked to {dockName} dock");
        return true;
    }

    private EntityUid? FindDockWithTag(string tag)
    {
        var query = EntityQueryEnumerator<PriorityDockComponent>();
        while (query.MoveNext(out var uid, out var dock))
        {
            if (dock.Tag == tag)
                return uid;
        }
        return null;
    }
}
