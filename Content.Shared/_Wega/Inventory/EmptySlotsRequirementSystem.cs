using Content.Shared.Inventory.Events;

namespace Content.Shared.Inventory;

public sealed partial class EmptySlotsRequirementSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmptySlotsRequirementComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, EmptySlotsRequirementComponent component, BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        CheckEmptySlotsRequirement((uid, component), args.EquipTarget, args);
    }

    private void CheckEmptySlotsRequirement(Entity<EmptySlotsRequirementComponent> ent, EntityUid target, EquipAttemptBase args)
    {
        var requiredSlots = ent.Comp.Slots;
        if (requiredSlots == SlotFlags.NONE)
            return;

        if (!TryComp<InventoryComponent>(target, out var inventory))
            return;

        var slots = inventory.Slots;
        foreach (var slot in slots)
        {
            if ((slot.SlotFlags & requiredSlots) == 0)
                continue;

            if (_inventory.TryGetSlotContainer(target, slot.Name, out var container, out _, inventory))
            {
                if (container.ContainedEntity != null)
                {
                    args.Reason = Loc.GetString("empty-slots-requirement-blocked");
                    args.Cancel();
                    return;
                }
            }
        }
    }
}
