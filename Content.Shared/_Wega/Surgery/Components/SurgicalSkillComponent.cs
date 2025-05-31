namespace Content.Shared.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgicalSkillComponent : Component
{
    [DataField("modifier")]
    public float Modifier = 0.5f;
}
