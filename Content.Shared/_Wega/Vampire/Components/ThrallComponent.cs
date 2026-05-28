using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Determines what the entity is and who it belongs to.
/// </summary>
[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ThrallComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? VampireOwner = default!;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ThrallFaction";
}
