using Content.Shared.Containers.ItemSlots;
using Content.Server.Power.Components;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Content.Shared.Popups;
using Content.Shared.Coordinates;

namespace Content.Server.Power.EntitySystems;

public sealed partial class ApcSystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;

    [SubscribeLocalEvent]
    private void OnItemSlotInsertAttempt(Entity<ApcComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

		if (args.User == null)
			return;

        if (!TryComp<WiresPanelComponent>(ent, out var panel))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CogSlotId, out var cogSlot) || cogSlot != args.Slot)
            return;

        if (!panel.Open || args.User == ent.Owner)
		{
			_popup.PopupEntity(Loc.GetString("ratvar-apc-no-open"), args.User.Value, args.User.Value);
            args.Cancelled = true;
            return;
		}
		
        var apc = _entityLookup.GetEntitiesInRange<InteractionCogInfectedComponent>(Transform(ent.Owner).Coordinates, 1.5f);
        if (apc.Count > 0)
        {
			_popup.PopupEntity(Loc.GetString("ratvar-apc-apc-near"), args.User.Value, args.User.Value);
            args.Cancelled = true;
		}
    }

    [SubscribeLocalEvent]
    private void OnItemSlotEjectAttempt(Entity<ApcComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

		if (args.User == null)
			return;

        if (!TryComp<WiresPanelComponent>(ent, out var panel))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CogSlotId, out var cogSlot) || cogSlot != args.Slot)
            return;

        if (!panel.Open || args.User == ent.Owner)
		{
			_popup.PopupEntity(Loc.GetString("ratvar-apc-no-open"), args.User.Value, args.User.Value);
            args.Cancelled = true;
		}
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
