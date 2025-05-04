namespace Content.Shared.Shadow.Components;

[RegisterComponent]
public sealed partial class ShadowMutateComponent : Component
{
    [DataField("damagePerLight")]
    public float DamagePerLight = 0.5f;

    [DataField("damageInterval")]
    public float DamageInterval = 1f;

    [DataField("nextDamageTime")]
    public TimeSpan NextDamageTime;
}
