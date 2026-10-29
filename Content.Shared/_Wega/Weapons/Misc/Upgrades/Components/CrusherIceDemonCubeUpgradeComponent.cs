using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherIceDemonCubeUpgradeComponent : Component
{
    [DataField] public EntProtoId SpawnProto = "MobIceDemonAfterimage";
    [DataField] public float SpawnProb = 0.1f;
    [DataField] public int SpawnCount = 2;
}
