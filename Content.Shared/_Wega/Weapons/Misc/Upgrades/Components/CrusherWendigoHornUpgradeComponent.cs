using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherWendigoHornUpgradeComponent : Component
{
    [DataField] public float DamageModifier = 2f;
}
