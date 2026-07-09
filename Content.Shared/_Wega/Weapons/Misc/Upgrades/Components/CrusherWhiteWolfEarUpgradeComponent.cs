using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherWhiteWolfEarUpgradeComponent : Component
{
    [DataField] public EntProtoId EffectProto = "BasicSpeedUpStatusEffect";
    [DataField] public TimeSpan Duration = TimeSpan.FromSeconds(1);
    [DataField] public float Modifier = 1.1f;
}
