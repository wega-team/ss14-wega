using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherDemonClawsUpgradeComponent : Component
{
    [DataField] public float DamageMultiplier = 0.15f;
}
