namespace Content.Shared.Vehicle.Components;

[RegisterComponent]
public sealed partial class BoatComponent : Component
{
    [DataField] public bool RequiredOal = true;
}
