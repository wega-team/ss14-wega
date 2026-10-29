using Content.Shared.Disease;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles;

[RegisterComponent]
public sealed partial class ProjectileInfectComponent : Component
{
    [DataField(required: true)]
    public ProtoId<DiseasePrototype> Infection;
    [DataField] public float Prob = 0.1f;
}
