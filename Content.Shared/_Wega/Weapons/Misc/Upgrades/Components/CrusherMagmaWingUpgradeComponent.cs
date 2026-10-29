using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherMagmaWingUpgradeComponent : Component
{
    [ViewVariables] public bool Active;
    [DataField(required: true)] public DamageSpecifier Damage;
}
