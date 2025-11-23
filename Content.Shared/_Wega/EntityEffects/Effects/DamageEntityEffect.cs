using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Applies damage to entities that have a specific required component.
/// The damage amount is equal to <see cref="Damage.Amount"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DamageEntityEffectSystem : EntityEffectSystem<DamageableComponent, Damage>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<Damage> args)
    {
        var componentType = EntityManager.ComponentFactory.GetRegistration(args.Effect.RequiredComponent).Type;
        if (!EntityManager.HasComponent(entity, componentType))
            return;

        var damageAmount = args.Effect.Amount * args.Scale;
        var damage = new DamageSpecifier();
        damage.DamageDict.Add(args.Effect.DamageType, damageAmount);

        _damageable.TryChangeDamage(entity.Owner, damage, true);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Damage : EntityEffectBase<Damage>
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageTypePrototype>))]
    public string DamageType = string.Empty;

    [DataField]
    public float Amount = 5f;

    [DataField(required: true)]
    public string RequiredComponent = string.Empty;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-damage-if-component",
            ("chance", Probability),
            ("damage", Amount),
            ("type", DamageType),
            ("component", RequiredComponent));
}
