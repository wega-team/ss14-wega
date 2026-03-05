using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherFrostGlandUpgradeComponent : Component
{
    [DataField] public float DamageModifier = 0.9f;
}
