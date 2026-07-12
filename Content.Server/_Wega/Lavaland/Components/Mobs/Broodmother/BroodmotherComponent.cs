namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(BroodmotherSystem))]
public sealed partial class BroodmotherComponent : Component
{
    public bool IsRaging = false;
    public bool IsPostRageSlow = false;

    public TimeSpan RageEndTime;
    public TimeSpan PostRageSlowEndTime;

    [DataField]
    public float BaseWalkSpeed = 1.75f;

    [DataField]
    public float BaseSprintSpeed = 1.75f;
}

[RegisterComponent, Access(typeof(BroodmotherSystem))]
public sealed partial class GoliathChildComponent : Component
{
    [ViewVariables] public EntityUid Mother;
}
