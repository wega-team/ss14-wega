using Robust.Shared.GameStates;

namespace Content.Shared.Inventory;

[Access(typeof(EmptySlotsRequirementSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmptySlotsRequirementComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SlotFlags Slots = SlotFlags.NONE;
}
