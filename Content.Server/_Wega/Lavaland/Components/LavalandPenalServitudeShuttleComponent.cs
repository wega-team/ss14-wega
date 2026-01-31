namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandPenalServitudeShuttleComponent : Component
{
    [ViewVariables]
    public PenalServitudeLavalandShuttleState State = PenalServitudeLavalandShuttleState.DockedAtStation;

    [ViewVariables]
    public TimeSpan? NextLaunchTime;
}

public enum PenalServitudeLavalandShuttleState : byte
{
    DockedAtStation,
    DockedAtPenalServitude,
    EnRouteToStation,
    EnRouteToPenalServitude
}
