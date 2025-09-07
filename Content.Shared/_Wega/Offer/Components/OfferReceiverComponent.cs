using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Offer;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedOfferItemSystem))]
public sealed partial class OfferReceiverComponent : Component
{
    [DataField]
    public EntityUid? Offerer;

    [DataField]
    public EntityUid? Item;

    public ProtoId<AlertPrototype> Alert = "Offer";
}
