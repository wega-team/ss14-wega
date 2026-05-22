using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// дает дополнительный урон ближним оружием
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class BonusMeleeDamageComponent : Component
{
    [DataField("bonusDamage")]
    public DamageSpecifier? BonusDamage;

    [DataField("damageModifierSet")]
    public DamageModifierSet? DamageModifierSet;

    [DataField("heavyDamageFlatModifier"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HeavyDamageFlatModifier;

    [DataField("heavyDamageMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float HeavyDamageMultiplier = 1;
}
