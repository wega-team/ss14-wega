namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileTimerResetsComponent : Component
{
    [DataField] public float ResetsTime = 0.5f;
}
