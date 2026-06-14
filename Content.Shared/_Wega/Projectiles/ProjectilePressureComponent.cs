namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectilePressureComponent : Component
{
    public bool Ignore = false;
    
    [DataField] public float DamageMultiplier = 3f;
}
