namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileAoEComponent : Component
{
    [DataField] public float DamageRadius = 3f;
    [DataField] public float DamageMultiplier = 0.2f;
}
