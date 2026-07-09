using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[Access(typeof(ClothingActionSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingActionComponent : Component
{
    [DataField("action", required: true)]
    public EntProtoId ClothingAction;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ClothingActionEntity;

    [DataField]
    public SlotFlags Slot = SlotFlags.WITHOUT_POCKET;

    [DataField]
    public bool InHands = false;
}
