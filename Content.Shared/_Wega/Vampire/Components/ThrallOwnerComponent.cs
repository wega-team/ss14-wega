using Robust.Shared.GameStates;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Determines whether an entity is the owner of the tralls and allows them to be manipulated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ThrallOwnerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int MaxThrallCount = 1;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int ThrallCount = 0;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<EntityUid> ThrallOwned = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool DamageSharing = false;
}
