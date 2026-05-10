using Robust.Shared.GameStates;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// The target is a supreme vampire.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SupremeVampireComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Active = false;
}
