namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class OperatingTableComponent : Component
{
    [DataField("modifier")]
    public float Modifier = 1f;
}
