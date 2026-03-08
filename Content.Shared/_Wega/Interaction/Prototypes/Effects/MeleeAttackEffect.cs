using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;

namespace Content.Shared.Interaction;

/// <summary>
/// An effect that simulates a light melee attack with the user's weapon.
/// The class takes the user's current weapon and performs an attack on the target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class MeleeAttackEffect : InteractionEffect
{
    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var meleeWeaponSystem = entityManager.System<SharedMeleeWeaponSystem>();
        if (!meleeWeaponSystem.TryGetWeapon(user, out var weapon, out var weaponComp))
            return;

        var combatSystem = entityManager.System<SharedCombatModeSystem>();

        var inCombat = combatSystem.IsInCombatMode(user);
        combatSystem.SetInCombatMode(user, true);

        if (!inCombat)
        {
            combatSystem.SetInCombatMode(user, false);
        }

        meleeWeaponSystem.AttemptLightAttack(user, weapon, weaponComp, target);
    }
}
