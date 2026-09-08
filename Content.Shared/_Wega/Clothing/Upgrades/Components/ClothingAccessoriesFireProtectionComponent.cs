using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingUpgradeEffectsSystem))]
public sealed partial class ClothingAccessoriesFireProtectionComponent : Component
{
    [DataField(required: true)]
    public float Reduction;
}
