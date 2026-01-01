namespace Content.Shared.EnergyShield;

[RegisterComponent]
public sealed partial class EnergyShieldOwnerComponent : Component
{
    [DataField]
    public EntityUid? ShieldEntity = null;

    [DataField]
    public int SustainingCount = 3;
}
