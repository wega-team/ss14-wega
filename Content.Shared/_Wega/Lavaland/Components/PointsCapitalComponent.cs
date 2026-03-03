namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class PointsCapitalComponent : Component
{
    [DataField]
    public double Points = 0;

    [DataField]
    public bool CardTransfer;
}
