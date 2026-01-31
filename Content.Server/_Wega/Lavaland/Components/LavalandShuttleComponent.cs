namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandShuttleComponent : Component
{
    [ViewVariables]
    public LavalandShuttleState State = LavalandShuttleState.DockedAtStation;

    [ViewVariables]
    public TimeSpan? NextLaunchTime;
}

public enum LavalandShuttleState : byte
{
    DockedAtStation,
    DockedAtOutpost,
    EnRouteToStation,
    EnRouteToOutpost
}
