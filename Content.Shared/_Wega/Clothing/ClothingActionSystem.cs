using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;

namespace Content.Shared.Clothing;

public sealed partial class ClothingActionSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingActionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ClothingActionComponent, GetItemActionsEvent>(OnGetItemActions);
    }

    private void OnMapInit(Entity<ClothingActionComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ClothingActionEntity, ent.Comp.ClothingAction);
        DirtyEntity(ent);
    }

    private void OnGetItemActions(Entity<ClothingActionComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
        {
            if (!ent.Comp.InHands)
                return;
        }
        else
        {
            if (!CheckSlot(ent.Comp, args.SlotFlags))
                return;
        }

        if (ent.Comp.ClothingActionEntity != null)
        {
            args.AddAction(ref ent.Comp.ClothingActionEntity, ent.Comp.ClothingAction);
        }
    }

    private bool CheckSlot(ClothingActionComponent comp, SlotFlags? currentSlot)
    {
        if (currentSlot == null)
            return true;

        return (comp.Slot & currentSlot.Value) != 0;
    }
}
