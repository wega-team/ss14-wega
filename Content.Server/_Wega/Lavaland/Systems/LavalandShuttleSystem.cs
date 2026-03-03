using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
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

public sealed partial class LavalandShuttleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly DockingSystem _dockingSystem = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private static readonly string DockStation = "DockLavalandStation";
    private static readonly string DockOutpost = "DockLavalandOutpost";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LavalandShuttleComponent, MapInitEvent>(OnShuttleMapInit);
        SubscribeLocalEvent<LavalandShuttleComponent, FTLCompletedEvent>(OnShuttleArrival);
        SubscribeLocalEvent<StationLavalandShuttleComponent, StationPostInitEvent>(OnStationStartup, after: [typeof(LavalandSystem)]);

        SubscribeLocalEvent<LavalandShuttleConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<LavalandShuttleConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<LavalandShuttleConsoleComponent, LavalandShuttleCallMessage>(OnShuttleCall);
    }

    private void OnShuttleMapInit(EntityUid uid, LavalandShuttleComponent component, MapInitEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<LavalandShuttleConsoleComponent>();
        while (consoleQuery.MoveNext(out var console, out var consoleComp))
        {
            consoleComp.ConnectedShuttle = uid;
            UpdateLocation(console, consoleComp);
        }
    }

    private void OnShuttleArrival(EntityUid uid, LavalandShuttleComponent component, ref FTLCompletedEvent args)
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
                    _ when oppositeDock.Tag == DockStation => LavalandShuttleState.DockedAtStation,
                    _ when oppositeDock.Tag == DockOutpost => LavalandShuttleState.DockedAtOutpost,
                    _ => component.State
                };

                UpdateConsoles();
                return;
            }
            else if (HasComp<DockingComponent>(dockComp.DockedWith))
            {
                var oppositeUid = dockComp.DockedWith.Value;
                var oppositeGrid = Transform(oppositeUid).GridUid;
                var oppositeMap = Transform(oppositeUid).MapUid;

                if (HasComp<LavalandComponent>(oppositeMap))
                {
                    component.State = LavalandShuttleState.DockedAtOutpost;
                    UpdateConsoles();
                    return;
                }

                if (oppositeGrid != null && HasComp<BecomesStationComponent>(oppositeGrid)
                    && !HasComp<StationCentcommComponent>(oppositeGrid))
                {
                    component.State = LavalandShuttleState.DockedAtStation;
                    UpdateConsoles();
                    return;
                }
            }
        }

        var mapUid = Transform(uid).MapUid;
        if (mapUid != null)
        {
            component.State = HasComp<LavalandComponent>(mapUid)
                ? LavalandShuttleState.DockedAtOutpost
                : LavalandShuttleState.DockedAtStation;

            UpdateConsoles();
            return;
        }

        Log.Warning($"Shuttle {ToPrettyString(uid)} arrived but no valid dock found!");
    }

    private void OnStationStartup(Entity<StationLavalandShuttleComponent> ent, ref StationPostInitEvent args)
    {
        if (!_cfg.GetCVar(WegaCVars.LavalandEnabled))
            return;

        Timer.Spawn(100, () => AddLavalandShuttle(ent));
    }

    private void AddLavalandShuttle(Entity<StationLavalandShuttleComponent> ent)
    {
        if (ent.Comp.LavalandShuttle != null)
        {
            if (Exists(ent.Comp.LavalandShuttle))
            {
                Log.Error($"Attempted to add a lavaland shuttle to {ToPrettyString(ent)}, despite a shuttle already existing?");
                return;
            }
            Log.Error($"Encountered deleted lavaland shuttle during initialization of {ToPrettyString(ent)}");
            ent.Comp.LavalandShuttle = null;
        }

        _map.CreateMap(out var tempMapId);

        if (!_loader.TryLoadGrid(tempMapId, ent.Comp.LavalandShuttlePath, out var shuttle))
        {
            Log.Error($"Unable to spawn lavaland shuttle {ent.Comp.LavalandShuttlePath} for {ToPrettyString(ent)}");
            _map.DeleteMap(tempMapId);
            return;
        }

        ent.Comp.LavalandShuttle = shuttle.Value;
        var shuttleComp = EnsureComp<LavalandShuttleComponent>(shuttle.Value);
        EnsureComp<ProtectedGridComponent>(shuttle.Value);

        bool docked = TryDockShuttle(shuttle.Value, DockStation, "station");
        if (!docked)
        {
            docked = TryDockShuttle(shuttle.Value, DockOutpost, "outpost");
            shuttleComp.State = LavalandShuttleState.DockedAtOutpost;
        }

        if (!docked)
        {
            Log.Error($"Failed to dock lavaland shuttle {ToPrettyString(shuttle)} to any available dock");
            _map.DeleteMap(tempMapId);
            ent.Comp.LavalandShuttle = null;
            return;
        }

        _map.DeleteMap(tempMapId);
        Log.Info($"Added lavaland shuttle {ToPrettyString(shuttle)} for station {ToPrettyString(ent)}");
    }

    private bool TryDockShuttle(EntityUid shuttle, string dockTag, string dockName)
    {
        var stationDock = FindDockWithTag(dockTag);
        if (stationDock == null)
        {
            Log.Warning($"Failed to find {dockName} dock with tag {dockTag} for lavaland shuttle {ToPrettyString(shuttle)}");
            return false;
        }

        var stationGrid = Transform(stationDock.Value).GridUid;
        if (stationGrid == null)
        {
            Log.Error($"Lavaland shuttle {ToPrettyString(shuttle)}: {dockName} dock {ToPrettyString(stationDock)} has no grid");
            return false;
        }

        var config = _dockingSystem.GetDockingConfig(shuttle, stationGrid.Value, dockTag);
        if (config == null)
        {
            Log.Warning($"Failed to find docking config for lavaland shuttle {ToPrettyString(shuttle)} at {dockName} dock {ToPrettyString(stationDock)}");
            return false;
        }

        var shuttleXform = Transform(shuttle);
        _shuttleSystem.FTLDock((shuttle, shuttleXform), config);
        Log.Info($"Lavaland shuttle {ToPrettyString(shuttle)} docked to {dockName} dock");
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
        // Trying docking on other docks
        var others = EntityQueryEnumerator<DockingComponent>();
        while (others.MoveNext(out var uid, out _))
        {
            if (tag == DockStation && HasComp<BecomesStationComponent>(Transform(uid).GridUid)
                && !HasComp<StationCentcommComponent>(Transform(uid).GridUid)) // No Centcomm docks!
                return uid;
            else if (tag == DockOutpost && HasComp<LavalandComponent>(Transform(uid).MapUid))
                return uid;
        }
        return null;
    }
}
