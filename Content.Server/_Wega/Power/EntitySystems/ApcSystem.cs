using Content.Shared.Containers.ItemSlots;
using Content.Server.Power.Components;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Server.Power.EntitySystems;

public sealed partial class ApcSystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    [SubscribeLocalEvent]
    private void OnItemSlotInsertAttempt(Entity<ApcComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<WiresPanelComponent>(ent, out var panel))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CogSlotId, out var cogSlot) || cogSlot != args.Slot)
            return;

        if (!panel.Open || args.User == ent.Owner)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnItemSlotEjectAttempt(Entity<ApcComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<WiresPanelComponent>(ent, out var panel))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CogSlotId, out var cogSlot) || cogSlot != args.Slot)
            return;

        if (!panel.Open || args.User == ent.Owner)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<ApcComponent> ent, ref EntInsertedIntoContainerMessage args)
    {

        if (args.Container == ent.Comp.CogSlot)
            EnsureComp<InteractionCogInfectedComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<ApcComponent> ent, ref EntRemovedFromContainerMessage args)
    {

        if (args.Container == ent.Comp.CogSlot && HasComp<InteractionCogInfectedComponent>(ent))
            RemComp<InteractionCogInfectedComponent>(ent);
    }
}
