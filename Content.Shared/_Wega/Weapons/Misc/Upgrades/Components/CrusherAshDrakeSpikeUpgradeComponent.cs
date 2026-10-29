using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherAshDrakeSpikeUpgradeComponent : Component
{
    [DataField] public float DamageRadius = 3f;
    [DataField] public float DamageMultiplier = 0.4f;
}
