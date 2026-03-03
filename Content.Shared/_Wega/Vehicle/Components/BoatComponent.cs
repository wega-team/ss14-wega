using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class BoatComponent : Component
{
    [DataField] public bool RequiredOal = true;
}
