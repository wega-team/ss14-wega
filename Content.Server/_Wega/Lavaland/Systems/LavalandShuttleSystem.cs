using System.Linq;
using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandShuttleSystem : EntitySystem
{
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
        SubscribeLocalEvent<StationLavalandShuttleComponent, StationPostInitEvent>(OnStationStartup);

        SubscribeLocalEvent<LavalandShuttleConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<LavalandShuttleConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<LavalandShuttleConsoleComponent, LavalandShuttleCallMessage>(OnShuttleCall);
    }

    private void OnShuttleMapInit(EntityUid uid, LavalandShuttleComponent component, MapInitEvent args)
    {
        var consoleQuery = EntityQueryEnumerator<LavalandShuttleConsoleComponent>();
        while (consoleQuery.MoveNext(out _, out var console))
        {
            console.ConnectedShuttle = uid;
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
                    _ when oppositeDock.Tag == DockStation => ShuttleState.DockedAtStation,
                    _ when oppositeDock.Tag == DockOutpost => ShuttleState.DockedAtOutpost,
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
                ? ShuttleState.DockedAtOutpost
                : ShuttleState.DockedAtStation;

            UpdateConsoles();
            return;
        }

        Log.Warning($"Shuttle {ToPrettyString(uid)} arrived but no valid dock found!");
    }

    private void OnStationStartup(Entity<StationLavalandShuttleComponent> ent, ref StationPostInitEvent args)
    {
        Timer.Spawn(100, () => AddLavalandShuttle(ent)); // Бля, в такие моменты вы не представляете как я люблю эту игру
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
        EnsureComp<LavalandShuttleComponent>(shuttle.Value);
        EnsureComp<ProtectedGridComponent>(shuttle.Value);

        var stationDock = FindDockWithTag(DockStation);
        if (stationDock != null && TryComp(shuttle, out TransformComponent? shuttleXform))
        {
            var stationGrid = Transform(stationDock.Value).GridUid;
            if (stationGrid == null)
            {
                Log.Error($"Lavaland shuttle {ToPrettyString(shuttle)} has no grid to dock at station dock {ToPrettyString(stationDock)}");
                _map.DeleteMap(tempMapId);
                return;
            }

            var config = _dockingSystem.GetDockingConfig(shuttle.Value, stationGrid.Value, DockStation);
            if (config == null)
            {
                Log.Error($"Failed to find docking config for lavaland shuttle {ToPrettyString(shuttle)} at station dock {ToPrettyString(stationDock)}");
                _map.DeleteMap(tempMapId);
                return;
            }
            _shuttleSystem.FTLDock((shuttle.Value, shuttleXform), config);
        }
        else
        {
            Log.Error($"Failed to find station dock {DockStation} for lavaland shuttle {ToPrettyString(shuttle)}");
            _map.DeleteMap(tempMapId);
            return;
        }

        _map.DeleteMap(tempMapId);

        Log.Info($"Added lavaland shuttle {ToPrettyString(shuttle)} for station {ToPrettyString(ent)}");
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
