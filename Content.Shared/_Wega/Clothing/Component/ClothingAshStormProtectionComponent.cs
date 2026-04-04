using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[Access(typeof(ClothingAshStormProtectionSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ClothingAshStormProtectionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 0.5f;
}
