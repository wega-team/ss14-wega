using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland;

[Serializable, NetSerializable]
public enum LavalandShuttleConsoleUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class LavalandShuttleConsoleState : BoundUserInterfaceState
{
    public ShuttleStatus Status;
    public DockLocation Location;
    public TimeSpan? NextLaunchTime;
    public bool CanCallShuttle;

    public LavalandShuttleConsoleState(ShuttleStatus status, DockLocation location, TimeSpan? nextLaunchTime, bool canCall)
    {
        Status = status;
        Location = location;
        NextLaunchTime = nextLaunchTime;
        CanCallShuttle = canCall;
    }
}

[Serializable, NetSerializable]
public sealed class LavalandShuttleCallMessage : BoundUserInterfaceMessage
{
    public bool ReturnCall;

    public LavalandShuttleCallMessage(bool returnCall = false)
    {
        ReturnCall = returnCall;
    }
}

public enum ShuttleStatus
{
    Unknown,
    Unavailable,
    DockedAtStation,
    DockedAtOutpost,
    EnRouteToStation,
    EnRouteToOutpost
}

public enum DockLocation
{
    Station,
    Outpost,
    Shuttle
}
