namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandShuttleComponent : Component
{
    [ViewVariables]
    public LavalandShuttleState State = LavalandShuttleState.DockedAtStation;

    [ViewVariables]
    public TimeSpan? NextLaunchTime;

    [DataField]
    public float LaunchDelay = 90f;
}

public enum LavalandShuttleState : byte
{
    DockedAtStation,
    DockedAtOutpost,
    EnRouteToStation,
    EnRouteToOutpost
}
