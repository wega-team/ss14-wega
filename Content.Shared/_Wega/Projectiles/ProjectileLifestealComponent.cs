using Content.Shared.FixedPoint;

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileLifestealComponent : Component
{
    [DataField] public FixedPoint2 StealAmount;
}
