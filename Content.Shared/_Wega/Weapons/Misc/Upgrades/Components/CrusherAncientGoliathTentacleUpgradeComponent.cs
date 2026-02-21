using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherAncientGoliathTentacleUpgradeComponent : Component
{
    [DataField] public float Coefficient = 0.5f;
    [DataField] public float HealModifier = 0.9f;
}
