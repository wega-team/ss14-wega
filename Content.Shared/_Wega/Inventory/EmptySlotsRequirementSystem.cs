using Content.Shared.Inventory.Events;
using Content.Shared.Modular.Suit;
using Content.Shared.Whitelist;

namespace Content.Shared.Inventory;

public sealed partial class EmptySlotsRequirementSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmptySlotsRequirementComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<EmptySlotsRequirementComponent, ModularSuitDeployAttemptEvent>(OnDeployAttempt);
    }

    private void OnEquipAttempt(EntityUid uid, EmptySlotsRequirementComponent component, BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        CheckEmptySlotsRequirement((uid, component), args.EquipTarget, args);
    }

    private void OnDeployAttempt(Entity<EmptySlotsRequirementComponent> ent, ref ModularSuitDeployAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ModularSuitComponent>(ent, out var suit) || suit.Wearer == null)
            return;

        CheckEmptySlotsRequirement(ent, suit.Wearer.Value, args);
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
                var item = container.ContainedEntity;
                if (item != null)
                {
                    if (ent.Comp.Blacklist != null && _whitelist.IsWhitelistFail(ent.Comp.Blacklist, item.Value))
                        return;

                    args.Reason = Loc.GetString("empty-slots-requirement-blocked");
                    args.Cancel();
                    return;
                }
            }
        }
    }

    private void CheckEmptySlotsRequirement(Entity<EmptySlotsRequirementComponent> ent, EntityUid target, CancellableEntityEventArgs args)
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
                var item = container.ContainedEntity;
                if (item != null)
                {
                    if (ent.Comp.Blacklist != null && _whitelist.IsWhitelistFail(ent.Comp.Blacklist, item.Value))
                        return;

                    args.Cancel();
                    return;
                }
            }
        }
    }
}
