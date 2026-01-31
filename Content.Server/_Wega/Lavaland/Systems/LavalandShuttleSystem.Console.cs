using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandShuttleSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void OnConsoleInit(EntityUid uid, LavalandShuttleConsoleComponent component, MapInitEvent args)
    {
        var shuttleQuery = EntityQueryEnumerator<LavalandShuttleComponent>();
        while (shuttleQuery.MoveNext(out var shuttleUid, out _))
        {
            component.ConnectedShuttle = shuttleUid;
            break;
        }

        if (component.ConnectedShuttle == null)
            return;

        if (HasComp<LavalandShuttleComponent>(Transform(uid).GridUid))
        {
            component.Location = DockLocation.Shuttle;
            component.ConnectedShuttle = Transform(uid).GridUid;
            return;
        }

        var consoleGrid = Transform(uid).GridUid;
        var consoleMap = Transform(uid).MapUid;

        if (consoleGrid != null)
        {
            if (HasComp<BecomesStationComponent>(consoleGrid)
                && !HasComp<StationCentcommComponent>(consoleGrid))
            {
                component.Location = DockLocation.Station;
                return;
            }
        }

        if (consoleMap != null && HasComp<LavalandComponent>(consoleMap))
        {
            component.Location = DockLocation.Outpost;
            return;
        }

        var dockQuery = EntityQueryEnumerator<PriorityDockComponent>();
        while (dockQuery.MoveNext(out var dockUid, out var dock))
        {
            if (Transform(dockUid).GridUid != Transform(uid).GridUid)
                continue;

            if (dock.Tag == DockStation)
            {
                component.Location = DockLocation.Station;
                break;
            }

            if (dock.Tag == DockOutpost)
            {
                component.Location = DockLocation.Outpost;
                break;
            }
        }
    }

    private void OnUiOpened(EntityUid uid, LavalandShuttleConsoleComponent comp, BoundUIOpenedEvent args)
    {
        if (comp.ConnectedShuttle == null)
        {
            var shuttleQuery = EntityQueryEnumerator<LavalandShuttleComponent>();
            while (shuttleQuery.MoveNext(out var shittle, out _))
            {
                comp.ConnectedShuttle = shittle;
                break;
            }
        }

        UpdateUI(uid, comp);
    }

    private void OnShuttleCall(EntityUid uid, LavalandShuttleConsoleComponent component, LavalandShuttleCallMessage args)
    {
        if (component.ConnectedShuttle == null || !TryComp<LavalandShuttleComponent>(component.ConnectedShuttle.Value, out var shuttle)
            || !TryComp<ShuttleComponent>(component.ConnectedShuttle.Value, out var shuttleComp))
            return;

        if (!CanCallShuttle(component, shuttle))
            return;

        string targetTag;
        LavalandShuttleState newState;

        if (component.Location == DockLocation.Station
            || component.Location == DockLocation.Shuttle && shuttle.State == LavalandShuttleState.DockedAtStation)
        {
            targetTag = DockOutpost;
            newState = LavalandShuttleState.EnRouteToOutpost;
        }
        else
        {
            targetTag = DockStation;
            newState = LavalandShuttleState.EnRouteToStation;
        }

        var targetDock = FindDockWithTag(targetTag);
        if (targetDock == null)
        {
            Log.Error($"Target dock with tag {targetTag} not found!");
            return;
        }

        var gridUid = Transform(targetDock.Value).GridUid;
        if (gridUid == null)
        {
            Log.Error($"grid on {targetDock} not found!");
            return;
        }

        _shuttleSystem.FTLToDock(component.ConnectedShuttle.Value, shuttleComp, gridUid.Value, priorityTag: targetTag);

        shuttle.State = newState;
        shuttle.NextLaunchTime = _gameTiming.CurTime + TimeSpan.FromSeconds(60f);
        UpdateConsoles();
    }

    private void UpdateConsoles()
    {
        var query = EntityQueryEnumerator<LavalandShuttleConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUI(uid, comp);
        }
    }

    private void UpdateUI(EntityUid consoleUid, LavalandShuttleConsoleComponent? component = null)
    {
        if (!Resolve(consoleUid, ref component))
            return;

        ShuttleStatus status;
        TimeSpan? launchTime = null;
        bool canCall = false;

        if (component.ConnectedShuttle.HasValue && TryComp<LavalandShuttleComponent>(component.ConnectedShuttle.Value, out var shuttle))
        {
            status = shuttle.State switch
            {
                LavalandShuttleState.DockedAtStation => ShuttleStatus.DockedAtStation,
                LavalandShuttleState.DockedAtOutpost => ShuttleStatus.DockedAtOutpost,
                LavalandShuttleState.EnRouteToStation => ShuttleStatus.EnRouteToStation,
                LavalandShuttleState.EnRouteToOutpost => ShuttleStatus.EnRouteToOutpost,
                _ => ShuttleStatus.Unknown
            };

            launchTime = shuttle.NextLaunchTime;
            canCall = CanCallShuttle(component, shuttle);
        }
        else
        {
            status = ShuttleStatus.Unavailable;
        }

        var state = new LavalandShuttleConsoleState(
            status,
            component.Location,
            launchTime,
            canCall
        );

        _ui.SetUiState(consoleUid, LavalandShuttleConsoleUiKey.Key, state);
    }

    private bool CanCallShuttle(LavalandShuttleConsoleComponent console, LavalandShuttleComponent shuttle)
    {
        if (shuttle.NextLaunchTime > _gameTiming.CurTime)
            return false;

        if (shuttle.State is not (LavalandShuttleState.DockedAtStation or LavalandShuttleState.DockedAtOutpost))
            return false;

        return console.Location == DockLocation.Station && shuttle.State == LavalandShuttleState.DockedAtStation
            || console.Location == DockLocation.Outpost && shuttle.State == LavalandShuttleState.DockedAtOutpost
            || console.Location == DockLocation.Shuttle;
    }
}
