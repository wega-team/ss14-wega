namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class OreProcessorPointsComponent : Component
{
    [DataField]
    public double AccumulatedPoints = 0;
	
    [DataField]
    public bool IsProm = false;
	
    [DataField]
    public float Multiplay = 2.0f;
}
