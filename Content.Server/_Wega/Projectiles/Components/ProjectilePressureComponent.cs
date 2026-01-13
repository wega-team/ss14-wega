namespace Content.Server.Projectiles;

[RegisterComponent]
public sealed partial class ProjectilePressureComponent : Component
{
    [DataField] public float DamageMultiplier = 3f;
}
