using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.Components;

namespace Content.Shared.Genetics;

[RegisterComponent, NetworkedComponent]
public sealed partial class TelekinesisGenComponent : Component
{
    [ValidatePrototypeId<EntityPrototype>, DataField("itemPrototype"), ViewVariables(VVAccess.ReadWrite)]
    public string ItemPrototype = "HandTelekinesisGun";

    [DataField("handId"), ViewVariables(VVAccess.ReadWrite)]
    public string HandId = "telekinesis-hand";

    [ViewVariables]
    public EntityUid? TelekinesisItem;

    [DataField]
    public HandLocation HandPos = HandLocation.Middle;
}