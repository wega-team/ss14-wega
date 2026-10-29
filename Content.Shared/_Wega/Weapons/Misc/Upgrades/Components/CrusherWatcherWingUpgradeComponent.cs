using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherWatcherWingUpgradeComponent : Component
{
    [DataField]
    public float ResetsTime = 1f;
}
