namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandShuttleComponent : Component
{
    [ViewVariables]
    public ShuttleState State = ShuttleState.DockedAtStation;

    [ViewVariables]
    public TimeSpan? NextLaunchTime;
}

public enum ShuttleState
{
    DockedAtStation,
    DockedAtOutpost,
    EnRouteToStation,
    EnRouteToOutpost
}
