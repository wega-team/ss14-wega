using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileSlowdownComponent : Component
{
    [DataField]
    public EntProtoId EffectProto = "SpecialSlowdownStatusEffect";
    [DataField] public TimeSpan Duration = TimeSpan.FromSeconds(3);
    [DataField] public float Multiplier = 0.75f;
}
