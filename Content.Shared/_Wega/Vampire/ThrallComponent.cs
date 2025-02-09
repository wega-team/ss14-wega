using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThrallComponent : Component
{
    [DataField]
    public EntityUid? VampireOwner = null;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ThrallFaction";
}
