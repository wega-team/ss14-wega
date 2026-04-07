using Content.Shared.Inventory.Events;
using Content.Shared.Skrell;
using Content.Shared.Inventory;
using Content.Shared.Humanoid;
using Content.Shared.MagicMirror;
using Robust.Shared.Timing;
using Content.Shared.Body;
using System.Linq;

namespace Content.Server.Skrell;

public sealed class SkrellSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkrellComponent, DidEquipEvent>(OnEquip);
        SubscribeLocalEvent<HairMarkingRemovedEvent>(OnRemoveSlot);
    }

    private void OnEquip(Entity<SkrellComponent> entity, ref DidEquipEvent args)
    {
        var slot = args.Slot;
        if (slot == "pocket3")
        {
            var item = args.Equipee;
            if (CheckCondition(entity))
            {
                Timer.Spawn(1, () => _inventorySystem.TryUnequip(item, slot, force: true));
            }
        }
    }

    private bool CheckCondition(EntityUid uid)
    {
        if (!HasComp<VisualBodyComponent>(uid))
            return false;

        return !_visualBody.TryGatherMarkingsData(uid, null, out _, out _, out var applied)
            || applied.Values.All(organMarkings => !organMarkings.ContainsKey(HumanoidVisualLayers.Hair)
            || organMarkings[HumanoidVisualLayers.Hair].Count == 0);
    }

    private void OnRemoveSlot(HairMarkingRemovedEvent args)
    {
        var target = GetEntity(args.Target);
        if (!HasComp<SkrellComponent>(target))
            return;

        if (_inventorySystem.TryGetSlotEntity(target, "pocket3", out _))
        {
            _inventorySystem.TryUnequip(target, "pocket3");
        }
    }
}
