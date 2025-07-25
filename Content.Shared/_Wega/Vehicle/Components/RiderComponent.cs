using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Added to people when they are riding in a vehicle
/// used mostly to keep track of them for entityquery.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    /// <summary>
    /// The vehicle this rider is currently riding.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Vehicle;

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class RiderComponentState : ComponentState
{
    public NetEntity? Entity;
}
