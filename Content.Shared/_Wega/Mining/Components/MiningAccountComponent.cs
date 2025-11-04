namespace Content.Shared.Mining.Components;

[RegisterComponent]
public sealed partial class MiningAccountComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Credits;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ResearchPoints;

    [ViewVariables(VVAccess.ReadOnly)]
    public MiningMode GlobalMode = MiningMode.Credits;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool GlobalActivation = false;
}
