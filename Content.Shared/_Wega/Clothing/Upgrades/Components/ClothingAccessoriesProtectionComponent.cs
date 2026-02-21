using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingUpgradeEffectsSystem))]
public sealed partial class ClothingAccessoriesProtectionComponent : Component
{
    [DataField]
    public DamageModifierSet Modifiers = new();
}
