using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class DamageEntityEffect : EntityEffect
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageTypePrototype>))]
    public string DamageType = string.Empty;

    [DataField]
    public float Amount = 5f;

    [DataField(required: true)]
    public string RequiredComponent = string.Empty;

    [DataField]
    public string? BlacklistedComponent;

    public override bool ShouldLog => true;
    public override LogImpact LogImpact => LogImpact.Medium;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-damage-if-component",
            ("damage", Amount),
            ("type", DamageType),
            ("component", RequiredComponent));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var uid = args.TargetEntity;

        var requiredComponentType = entMan.ComponentFactory.GetRegistration(RequiredComponent).Type;
        if (!entMan.HasComponent(uid, requiredComponentType))
            return;

        if (!string.IsNullOrEmpty(BlacklistedComponent))
        {
            var blacklistedComponentType = entMan.ComponentFactory.GetRegistration(BlacklistedComponent).Type;
            if (entMan.HasComponent(uid, blacklistedComponentType))
                return;
        }

        if (entMan.TryGetComponent<DamageableComponent>(uid, out _))
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add(DamageType, Amount);
            entMan.System<DamageableSystem>().TryChangeDamage(uid, damage, true);
        }
    }
}
