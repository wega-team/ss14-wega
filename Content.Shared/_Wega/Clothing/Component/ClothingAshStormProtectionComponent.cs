using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingAshStormProtectionSystem))]
public sealed partial class ClothingAshStormProtectionComponent : Component
{
    [DataField]
    public float Modifier = 0.5f;
}
