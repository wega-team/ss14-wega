namespace Content.Server.Stunnable.Components;


[RegisterComponent]
public sealed partial class KnockdownOnHitComponent : Component
{
    [DataField]
    public bool KnockdownBorgs = false;
    
    [DataField]
    public bool Refresh = true;
    
    [DataField]
    public bool AutoStand = true;
    
    [DataField]
    public bool DropItems = false;
    
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(3);
}