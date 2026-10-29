using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherPoisonFangUpgradeComponent : Component
{
    [DataField] public float DamageModifier = 0.1f;
    [DataField] public float Interval = 2f;
}
