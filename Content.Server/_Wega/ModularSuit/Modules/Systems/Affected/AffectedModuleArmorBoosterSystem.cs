using Content.Shared.Armor;
using Content.Shared.Clothing.Upgrades;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Modular.Suit;

namespace Content.Server.Modular.Suit;

public sealed class AffectedModuleArmorBoosterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AffectedModuleArmorBoosterComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery,
            after: [typeof(SharedArmorSystem), typeof(ClothingUpgradeSystem)]);
        SubscribeLocalEvent<AffectedModuleArmorBoosterComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify,
            after: [typeof(SharedArmorSystem), typeof(ClothingUpgradeSystem)]);
    }

    private void OnCoefficientQuery(Entity<AffectedModuleArmorBoosterComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        foreach (var (key, value) in ent.Comp.Modifiers.Coefficients)
        {
            if (args.Args.DamageModifiers.Coefficients.TryGetValue(key, out var existing))
                args.Args.DamageModifiers.Coefficients[key] = existing * value;
            else
                args.Args.DamageModifiers.Coefficients[key] = value;
        }
    }

    private void OnDamageModify(Entity<AffectedModuleArmorBoosterComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.Modifiers);
    }
}
