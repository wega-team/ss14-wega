using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherVortexTalismanUpgradeComponent : Component
{
    [DataField] public EntProtoId SpawnProto = "WallHierophantTemporary";
    [DataField] public int SpawnCount = 3;
}
