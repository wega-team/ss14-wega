using Content.Shared.Inventory;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks the availability of items in the specified target equipment slots.
/// If the list of slots is empty, the condition is considered met.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ClothingPresentCondition : InteractionCondition
{
    /// <summary>
    /// A list of names of inventory slots to check.
    /// </summary>
    [DataField("slots")]
    public List<string> Slots { get; private set; } = new();

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (Slots.Count == 0)
            return true;

        var inventorySystem = entityManager.System<InventorySystem>();
        foreach (var slot in Slots)
        {
            if (!inventorySystem.TryGetSlotEntity(target, slot, out _))
                return false;
        }

        return true;
    }
}
