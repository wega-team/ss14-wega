using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherTheThingGlobUpgradeComponent : Component
{
    [DataField] public FixedPoint2 BaseHealAmount = FixedPoint2.New(4);
    [DataField] public FixedPoint2 MarkHealAmount = FixedPoint2.New(10);
}
