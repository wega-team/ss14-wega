namespace Content.Server.Stunnable.Components;


[RegisterComponent]
public sealed partial class MeleeThrowOnHitComponent : Component
{
    [Datafield]
    public bool KnockdownBorgs = false;
    
    [Datafield]
    public bool Refresh = true;
    
    [Datafield]
    public bool AutoStand = true;
    
    [Datafield]
    public bool DropItems = false;
    
    [Datafield]
    public float Time = 3f;
}