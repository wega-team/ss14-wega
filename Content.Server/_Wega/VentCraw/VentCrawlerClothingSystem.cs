using Content.Shared.Inventory.Events;
using Content.Shared.VentCraw;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Shared.Player;

namespace Content.Server.VentCraw;

public sealed partial class VentCrawlerClothingSystem : EntitySystem
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;

	private readonly string[] requiredClothingSlots = new[] { "belt", "outerClothing", "back"};

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerClothingComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<VentCrawlerClothingComponent, GotUnequippedEvent>(OnDidUnequip);
    }

    private void OnDidEquip(Entity<VentCrawlerClothingComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.User = args.EquipTarget;
		
        if (HasComp<VentCrawlerComponent>(args.EquipTarget))
        {
            ent.Comp.AlreadyHas = true;
            return;
        }

        ent.Comp.AlreadyHas = false;
		
		if (IsAnySlotOccupied(args.EquipTarget))
		{
			var message = Loc.GetString("interaction-slot-occupied-message");
			if (_entManager.TryGetComponent<ActorComponent>(args.EquipTarget, out var actor))
				_popupSystem.PopupEntity(message, args.EquipTarget, actor.PlayerSession, PopupType.Small);
			
			return;
		}

		EnsureComp<VentCrawlerComponent>(args.EquipTarget);
    }

	public override void Update(float frameTime)
	{
		base.Update(frameTime);

		var query = EntityQueryEnumerator<VentCrawlerClothingComponent>();
		while (query.MoveNext(out var uid, out var clothingComp))
		{
			var wearer = clothingComp.User;
				
			if (wearer == null || !wearer.Value.IsValid())
				continue;
				
			if (clothingComp.AlreadyHas)
				continue;
			
			bool hasSlot = IsAnySlotOccupied(wearer.Value);
			bool hasComp = HasComp<VentCrawlerComponent>(wearer.Value);
			
			if (hasSlot && hasComp)
			{
				RemComp<VentCrawlerComponent>(wearer.Value);
				
				var message = Loc.GetString("interaction-slot-occupied-message");
				if (_entManager.TryGetComponent<ActorComponent>(wearer.Value, out var actor))
					_popupSystem.PopupEntity(message, wearer.Value, actor.PlayerSession, PopupType.Small);
			}
			else if (!hasSlot && !hasComp)
			{
				EnsureComp<VentCrawlerComponent>(wearer.Value);
			}
		}
	}
	
    private void OnDidUnequip(Entity<VentCrawlerClothingComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.User = null;
		
        if (ent.Comp.AlreadyHas)
            return;

        RemComp<VentCrawlerComponent>(args.EquipTarget);
    }
	
	private bool IsAnySlotOccupied(EntityUid target)
	{
		if (TryComp<InventoryComponent>(target, out var inventory))
		{
			foreach (var slot in requiredClothingSlots)
			{
				if (_inventorySystem.TryGetSlotEntity(target, slot, out _, inventory))
				{
					return true;
				}
			}
		}
		return false;
	}
}
