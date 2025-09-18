using Content.Server.Lavaland.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Lavaland;
using Content.Shared.Lavaland.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class LavalandShuttleSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void OnConsoleInit(EntityUid uid, LavalandShuttleConsoleComponent component, ComponentInit args)
    {
        if (HasComp<LavalandShuttleComponent>(Transform(uid).GridUid))
        {
            component.Location = DockLocation.Shuttle;
            component.ConnectedShuttle = Transform(uid).GridUid;
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
        ShuttleState newState;

        if (component.Location == DockLocation.Station
            || component.Location == DockLocation.Shuttle && shuttle.State == ShuttleState.DockedAtStation)
        {
            targetTag = DockOutpost;
            newState = ShuttleState.EnRouteToOutpost;
        }
        else
        {
            targetTag = DockStation;
            newState = ShuttleState.EnRouteToStation;
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
                ShuttleState.DockedAtStation => ShuttleStatus.DockedAtStation,
                ShuttleState.DockedAtOutpost => ShuttleStatus.DockedAtOutpost,
                ShuttleState.EnRouteToStation => ShuttleStatus.EnRouteToStation,
                ShuttleState.EnRouteToOutpost => ShuttleStatus.EnRouteToOutpost,
                _ => ShuttleStatus.Unknown
            };

            launchTime = shuttle.NextLaunchTime;

            // Can call if shuttle is docked at current location and not on cooldown
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
        if (shuttle.State is not (ShuttleState.DockedAtStation or ShuttleState.DockedAtOutpost))
            return false;

        return console.Location == DockLocation.Station && shuttle.State == ShuttleState.DockedAtStation
            || console.Location == DockLocation.Outpost && shuttle.State == ShuttleState.DockedAtOutpost
            || console.Location == DockLocation.Shuttle;
    }
}