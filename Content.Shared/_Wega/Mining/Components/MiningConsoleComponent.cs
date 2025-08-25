namespace Content.Shared.Mining.Components;

[RegisterComponent]
public sealed partial class MiningConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? LinkedServer;
}
