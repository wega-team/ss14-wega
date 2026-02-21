namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class OreProcessorPointsComponent : Component
{
    [DataField]
    public double AccumulatedPoints = 0;
}
