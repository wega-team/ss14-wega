namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class ClothingSterilityComponent : Component
{
    [DataField("modifier")]
    public float Modifier = 1f;
}
