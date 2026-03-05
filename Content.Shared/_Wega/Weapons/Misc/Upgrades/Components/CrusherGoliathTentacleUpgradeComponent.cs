using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherGoliathTentacleUpgradeComponent : Component
{
    [DataField]
    public float MaxCoefficient = 1f;

    [DataField]
    public MobState TargetState = MobState.Critical;
}
