namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed partial class NinjaEnergyCloneComponent : Component
{
    [DataField]
    public EntityUid Ninja;

    public float TargetScanTimer;

    /// Tracks last observed NextAttack to detect when a melee swing occurred.
    public TimeSpan LastNextAttack;
}
