namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileTeleportComponent : Component
{
    /// <summary>
    /// If true, this projectile can only teleport the shooter once.
    /// </summary>
    [DataField]
    public bool UseOnCollide = true;

    public bool Used = false;
}
