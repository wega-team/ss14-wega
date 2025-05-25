using Content.Shared.Inventory;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class ClothingCondition : SurgeryStepCondition
{
    [DataField("slots")]
    public List<string> Slots { get; private set; } = new();

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (Slots.Count == 0)
            return true;

        var inventorySystem = entityManager.System<InventorySystem>();
        foreach (var slot in Slots)
        {
            if (!inventorySystem.TryGetSlotEntity(patient, slot, out _))
                return false;
        }

        return true;
    }
}