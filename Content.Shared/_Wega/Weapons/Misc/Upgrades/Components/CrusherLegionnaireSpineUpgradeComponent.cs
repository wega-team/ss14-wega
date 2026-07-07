using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherLegionnaireSpineUpgradeComponent : Component
{
    [DataField] public EntProtoId SpawnProto = "MobLegionSkull";
    [DataField] public float SpawnProb = 0.2f;
}
