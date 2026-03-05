using Robust.Shared.Serialization;

namespace Content.Shared.Lavaland;

[Serializable, NetSerializable]
public enum LavalandShuttleConsoleUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PenalServitudeLavalandShuttleConsoleUiKey
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
public sealed class PenalServitudeLavalandShuttleConsoleState : BoundUserInterfaceState
{
    public PenalServitudeLavalandShuttleStatus Status;
    public PenalServitudeLavalandDockLocation Location;
    public TimeSpan? NextLaunchTime;
    public bool CanCallShuttle;

    public PenalServitudeLavalandShuttleConsoleState(PenalServitudeLavalandShuttleStatus status, PenalServitudeLavalandDockLocation location, TimeSpan? nextLaunchTime, bool canCall)
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

[Serializable, NetSerializable]
public sealed class PenalServitudeLavalandShuttleCallMessage : BoundUserInterfaceMessage
{
    public bool ReturnCall;

    public PenalServitudeLavalandShuttleCallMessage(bool returnCall = false)
    {
        ReturnCall = returnCall;
    }
}

public enum ShuttleStatus : byte
{
    Unknown,
    Unavailable,
    DockedAtStation,
    DockedAtOutpost,
    EnRouteToStation,
    EnRouteToOutpost
}

public enum PenalServitudeLavalandShuttleStatus : byte
{
    Unknown,
    Unavailable,
    DockedAtStation,
    DockedAtPenalServitude,
    EnRouteToStation,
    EnRouteToPenalServitude
}

public enum DockLocation : byte
{
    Station,
    Outpost,
    Shuttle
}

public enum PenalServitudeLavalandDockLocation : byte
{
    Station,
    PenalServitude,
    Shuttle
}
