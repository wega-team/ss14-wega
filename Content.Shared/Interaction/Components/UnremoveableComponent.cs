using Content.Shared.Inventory; // Corvax-Wega-UnremoveableOnlyInSlots
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnremoveableComponent : Component
{
    /// <summary>
    /// If this is true then unremovable items that are removed from inventory are deleted (typically from corpse gibbing).
    /// Items within unremovable containers are not deleted when removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteOnDrop = true;

    // Corvax-Wega-UnremoveableOnlyInSlots-start
    /// <summary>
    /// If set, the item is unremovable only in an inventory slot with these flags.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags OnlyInSlots = SlotFlags.NONE;
    // Corvax-Wega-UnremoveableOnlyInSlots-end
}
