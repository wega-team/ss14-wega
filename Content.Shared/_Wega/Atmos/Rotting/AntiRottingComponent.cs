namespace Content.Shared.Atmos.Rotting;

[RegisterComponent, Access(typeof(SharedRottingSystem))]
public sealed partial class AntiRottingComponent : Component
{
    [DataField]
    public float SlowdownFactor = 0.3f;

    [DataField]
    public TimeSpan ExpiryTime;
}