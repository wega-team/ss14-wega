namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandShuttleConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedShuttle;

    [ViewVariables]
    public DockLocation Location = DockLocation.Station;
}
