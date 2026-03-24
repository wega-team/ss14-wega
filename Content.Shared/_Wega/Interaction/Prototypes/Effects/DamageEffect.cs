using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of dealing damage.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class DamageEffect : InteractionEffect
{
    /// <summary>
    /// The amount of damage dealt. <see cref="DamageSpecifier"/>
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier TargetDamage { get; private set; }

    /// <summary>
    /// Ignore target resistance when calculating damage.
    /// </summary>
    [DataField]
    public bool IgnoreResistances { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var damageSystem = entityManager.System<DamageableSystem>();

        var positive = !TargetDamage.AnyPositive();
        damageSystem.TryChangeDamage(target, TargetDamage, IgnoreResistances, positive, user);
    }
}
