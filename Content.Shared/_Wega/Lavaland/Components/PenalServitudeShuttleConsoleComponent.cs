namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class PenalServitudeShuttleConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedShuttle;

    [ViewVariables]
    public PenalServitudeLavalandDockLocation Location = PenalServitudeLavalandDockLocation.Station;
}
