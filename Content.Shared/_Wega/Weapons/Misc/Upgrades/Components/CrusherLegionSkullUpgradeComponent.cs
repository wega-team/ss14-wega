using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherLegionSkullUpgradeComponent : Component
{
    [DataField]
    public float Coefficient = 1.3f;
}
