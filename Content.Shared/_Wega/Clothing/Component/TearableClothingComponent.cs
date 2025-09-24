namespace Content.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class TearableClothingComponent : Component
{
    [DataField]
    public float Delay = 8f;
}
