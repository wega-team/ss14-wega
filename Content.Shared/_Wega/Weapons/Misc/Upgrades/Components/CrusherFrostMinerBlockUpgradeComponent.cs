using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherFrostMinerBlockUpgradeComponent : Component
{
    [DataField] public EntProtoId EffectProto = "SpecialSlowdownStatusEffect";
    [DataField] public TimeSpan Duration = TimeSpan.FromSeconds(1.5f);
    [DataField] public float SpeedModifdier = 0.15f;
}
