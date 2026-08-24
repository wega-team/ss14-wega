namespace Content.Shared.Gatherable.Components;

[RegisterComponent, Access(typeof(GatherableSystem))]
public sealed partial class PendingGatherComponent : Component
{
    [DataField]
    public int Hits;
}
