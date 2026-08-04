using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Placed on the energy net entity itself. Stores which mob is currently trapped.
/// When this entity is deleted (by damage or timer), the system removes
/// <see cref="EnergyNettedComponent"/> from the stored target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnergyNetComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
