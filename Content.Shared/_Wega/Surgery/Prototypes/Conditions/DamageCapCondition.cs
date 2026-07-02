using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class DamageCapCondition : SurgeryStepCondition
{
    [DataField("damageCount", required: true)]
    public int DamageCap = 100;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<DamageableComponent>(patient, out var damageableComp) || !entityManager.HasComponent<InjurableComponent>(patient))
            return false;

        var damageable = entityManager.System<DamageableSystem>();
        var totalDamage = damageable.GetTotalDamage((patient, damageableComp));
        if (DamageCap != 0 && totalDamage >= DamageCap)
            return true;

        return false;
    }
}
