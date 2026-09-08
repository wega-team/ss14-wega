using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherPolarBearPawUpgradeComponent : Component
{
    [DataField] public MobState TargetState = MobState.PreCritical;
    [DataField] public float Threshold = 0.5f;
}
