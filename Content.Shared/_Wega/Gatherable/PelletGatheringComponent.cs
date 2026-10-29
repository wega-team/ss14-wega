namespace Content.Shared.Gatherable.Components;

[RegisterComponent]
public sealed partial class PelletGatheringComponent : Component
{
    [DataField]
    public int HitsRequired = 3;
}
