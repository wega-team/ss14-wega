using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandPenalServitudeShuttleSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void OnConsoleInit(EntityUid uid, PenalServitudeShuttleConsoleComponent component, MapInitEvent args)
    {
        UpdateLocation(uid, component);
    }

    private void OnUiOpened(EntityUid uid, PenalServitudeShuttleConsoleComponent comp, BoundUIOpenedEvent args)
    {
        if (comp.ConnectedShuttle == null)
        {
            var shuttleQuery = EntityQueryEnumerator<LavalandPenalServitudeShuttleComponent>();
            while (shuttleQuery.MoveNext(out var shuttle, out _))
            {
                comp.ConnectedShuttle = shuttle;
                break;
            }
        }

        UpdateUI(uid, comp);
    }

    private void OnShuttleCall(EntityUid uid, PenalServitudeShuttleConsoleComponent component, PenalServitudeLavalandShuttleCallMessage args)
    {
        if (component.ConnectedShuttle == null || !TryComp<LavalandPenalServitudeShuttleComponent>(component.ConnectedShuttle.Value, out var shuttle)
            || !TryComp<ShuttleComponent>(component.ConnectedShuttle.Value, out var shuttleComp))
            return;

        if (!CanCallShuttle(component, shuttle))
            return;

        string targetTag;
        PenalServitudeLavalandShuttleState newState;

        if (shuttle.State == PenalServitudeLavalandShuttleState.DockedAtStation)
        {
            targetTag = DockPenalServitude;
            newState = PenalServitudeLavalandShuttleState.EnRouteToPenalServitude;
        }
        else if (shuttle.State == PenalServitudeLavalandShuttleState.DockedAtPenalServitude)
        {
            targetTag = DockStation;
            newState = PenalServitudeLavalandShuttleState.EnRouteToStation;
        }
        else
        {
            return;
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
        shuttle.NextLaunchTime = _gameTiming.CurTime + TimeSpan.FromSeconds(shuttle.LaunchDelay);
        UpdateConsoles();
    }

    private void UpdateConsoles()
    {
        var query = EntityQueryEnumerator<PenalServitudeShuttleConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUI(uid, comp);
        }
    }

    private void UpdateLocation(EntityUid consoleUid, PenalServitudeShuttleConsoleComponent? component = null)
    {
        if (!Resolve(consoleUid, ref component))
            return;

        if (Transform(consoleUid).GridUid is { } grid && HasComp<LavalandPenalServitudeShuttleComponent>(grid))
        {
            component.Location = PenalServitudeLavalandDockLocation.Shuttle;
            component.ConnectedShuttle = grid;
            return;
        }

        var shuttleQuery = EntityQueryEnumerator<LavalandPenalServitudeShuttleComponent>();
        while (shuttleQuery.MoveNext(out var shuttle, out _))
        {
            component.ConnectedShuttle = shuttle;
            break;
        }

        if (component.ConnectedShuttle == null)
            return;

        var dockQuery = EntityQueryEnumerator<PriorityDockComponent>();
        while (dockQuery.MoveNext(out var dockUid, out var dock))
        {
            if (Transform(dockUid).GridUid != Transform(consoleUid).GridUid)
                continue;

            if (dock.Tag == DockStation)
            {
                component.Location = PenalServitudeLavalandDockLocation.Station;
                break;
            }

            if (dock.Tag == DockPenalServitude)
            {
                component.Location = PenalServitudeLavalandDockLocation.PenalServitude;
                break;
            }
        }
    }

    private void UpdateUI(EntityUid consoleUid, PenalServitudeShuttleConsoleComponent? component = null)
    {
        if (!Resolve(consoleUid, ref component))
            return;

        PenalServitudeLavalandShuttleStatus status;
        TimeSpan? launchTime = null;
        bool canCall = false;

        if (component.ConnectedShuttle.HasValue && TryComp<LavalandPenalServitudeShuttleComponent>(component.ConnectedShuttle.Value, out var shuttle))
        {
            status = shuttle.State switch
            {
                PenalServitudeLavalandShuttleState.DockedAtStation => PenalServitudeLavalandShuttleStatus.DockedAtStation,
                PenalServitudeLavalandShuttleState.DockedAtPenalServitude => PenalServitudeLavalandShuttleStatus.DockedAtPenalServitude,
                PenalServitudeLavalandShuttleState.EnRouteToStation => PenalServitudeLavalandShuttleStatus.EnRouteToStation,
                PenalServitudeLavalandShuttleState.EnRouteToPenalServitude => PenalServitudeLavalandShuttleStatus.EnRouteToPenalServitude,
                _ => PenalServitudeLavalandShuttleStatus.Unknown
            };

            launchTime = shuttle.NextLaunchTime;
            canCall = CanCallShuttle(component, shuttle);
        }
        else
        {
            status = PenalServitudeLavalandShuttleStatus.Unavailable;
        }

        var state = new PenalServitudeLavalandShuttleConsoleState(
            status,
            component.Location,
            launchTime,
            canCall
        );

        _ui.SetUiState(consoleUid, PenalServitudeLavalandShuttleConsoleUiKey.Key, state);
    }

    private bool CanCallShuttle(PenalServitudeShuttleConsoleComponent console, LavalandPenalServitudeShuttleComponent shuttle)
    {
        if (shuttle.NextLaunchTime > _gameTiming.CurTime)
            return false;

        if (shuttle.State is not (PenalServitudeLavalandShuttleState.DockedAtStation or PenalServitudeLavalandShuttleState.DockedAtPenalServitude))
            return false;

        return console.Location == PenalServitudeLavalandDockLocation.Shuttle
            || console.Location == PenalServitudeLavalandDockLocation.Station
            || console.Location == PenalServitudeLavalandDockLocation.PenalServitude;
    }
}
