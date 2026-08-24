namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class WieldSpreadModifierComponent : Component
{
    [DataField]
    public float Wielded = 0.5f;

    [DataField]
    public float Unwielded = 1f;
}
