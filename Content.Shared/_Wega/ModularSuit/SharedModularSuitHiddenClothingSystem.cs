using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Atmos;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;

namespace Content.Shared.Modular.Suit;

/// <summary>
/// Use this if you need to transfer effects from clothing hidden under a suit.
/// </summary>
public abstract partial class SharedModularSuitHiddenClothingSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Protection
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<GetFireProtectionEvent>>(OnGetFireProtection);
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<ElectrocutionAttemptEvent>>(OnElectrocutionAttempt);

        // Movement
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeed);
    }

    private void OnDamageModify(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<ArmorComponent>(item, out var armor))
            {
                if (TryComp<MaskComponent>(item, out var mask) && mask.IsToggled)
                    continue;

                var weakenedModifiers = new DamageModifierSet
                {
                    Coefficients = armor.Modifiers.Coefficients.ToDictionary(
                        x => x.Key, x => 1f - (1f - x.Value) * ent.Comp.ArmorEfficiency),
                    FlatReduction = armor.Modifiers.FlatReduction.ToDictionary(
                        x => x.Key, x => x.Value * ent.Comp.ArmorEfficiency)
                };

                args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, weakenedModifiers);
            }
        }
    }

    private void OnCoefficientQuery(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<ArmorComponent>(item, out var armor))
            {
                if (TryComp<MaskComponent>(item, out var mask) && mask.IsToggled)
                    continue;

                foreach (var armorCoefficient in armor.Modifiers.Coefficients)
                {
                    var weakenedCoefficient = 1f - (1f - armorCoefficient.Value) * ent.Comp.ArmorEfficiency;

                    args.Args.DamageModifiers.Coefficients[armorCoefficient.Key] =
                        args.Args.DamageModifiers.Coefficients.TryGetValue(armorCoefficient.Key, out var coefficient)
                            ? coefficient * weakenedCoefficient : weakenedCoefficient;
                }
            }
        }
    }

    private void OnGetFireProtection(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<GetFireProtectionEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<FireProtectionComponent>(item, out var fireProtection))
                args.Args.Reduce(fireProtection.Reduction);
        }
    }

    private void OnElectrocutionAttempt(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<ElectrocutionAttemptEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<InsulatedComponent>(item, out var insulated))
                args.Args.SiemensCoefficient *= insulated.Coefficient;
        }
    }

    private void OnRefreshMovementSpeed(Entity<ModularSuitHiddenClothingComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<ClothingSpeedModifierComponent>(item, out var speedMod))
            {
                if (speedMod.RequireActivated && !_toggle.IsActivated(item))
                    continue;

                if (speedMod.Standing != null && !_standing.IsMatchingState(args.Owner, speedMod.Standing.Value))
                    continue;

                args.Args.ModifySpeed(speedMod.WalkModifier, speedMod.SprintModifier);
            }
        }
    }
}
