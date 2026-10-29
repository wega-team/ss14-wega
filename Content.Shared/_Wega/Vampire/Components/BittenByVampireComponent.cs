namespace Content.Shared.Vampire.Components;

/// <summary>
/// A mark that allows you to see the presence of fang marks on the victim.
/// </summary>
[RegisterComponent]
public sealed partial class BittenByVampireComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExpirationTime { get; set; }

    [DataField]
    public float LifetimeSeconds = 900f;
}
