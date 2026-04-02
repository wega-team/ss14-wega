using Content.Shared.Atmos;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
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
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<GetFireProtectionEvent>>(OnGetFireProtection);
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<ElectrocutionAttemptEvent>>(OnElectrocutionAttempt);

        // Movement
        SubscribeLocalEvent<ModularSuitHiddenClothingComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeed);
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
        if (!TryComp<ClothingSpeedModifierComponent>(ent.Owner, out var suitSpeedMod))
            return;

        foreach (var (_, item) in ent.Comp.HiddenItems)
        {
            if (TryComp<ClothingSpeedModifierComponent>(item, out var speedMod))
            {
                if (speedMod.RequireActivated && !_toggle.IsActivated(item))
                    continue;

                if (speedMod.Standing != null && !_standing.IsMatchingState(args.Owner, speedMod.Standing.Value))
                    continue;

                // The suit no longer provides a passive speed baff
                var walkMod = Math.Min(suitSpeedMod.WalkModifier, speedMod.WalkModifier);
                var sprintMod = Math.Min(suitSpeedMod.SprintModifier, speedMod.SprintModifier);

                args.Args.ModifySpeed(walkMod, sprintMod);
            }
        }
    }
}
