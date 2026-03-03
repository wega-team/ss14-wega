using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.Clothing.Upgrades;

public sealed class ClothingUpgradeEffectsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingAccessoriesProtectionComponent, CoefficientQueryEvent>(OnCoefficientQuery);
        SubscribeLocalEvent<ClothingAccessoriesProtectionComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnCoefficientQuery(Entity<ClothingAccessoriesProtectionComponent> ent, ref CoefficientQueryEvent args)
    {
        foreach (var (key, value) in ent.Comp.Modifiers.Coefficients)
        {
            if (args.DamageModifiers.Coefficients.TryGetValue(key, out var existing))
                args.DamageModifiers.Coefficients[key] = existing * value;
            else
                args.DamageModifiers.Coefficients[key] = value;
        }
    }

    private void OnDamageModify(Entity<ClothingAccessoriesProtectionComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifiers);
    }
}
