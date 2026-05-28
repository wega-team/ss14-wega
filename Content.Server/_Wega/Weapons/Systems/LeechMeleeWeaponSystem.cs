using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Server.Weapons.Marker;

public sealed class LeechMeleeWeaponSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LeechMeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, LeechMeleeWeaponComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        DamageSpecifier? heal = component.Heal?.Clone();
        DamageSpecifier? groupsHeal = null;

        if (component.HealGroups != null && !component.HealGroups.Empty)
        {
            if (component.Weighted)
            {
                groupsHeal = _damageable.CreateWeightedHealFromGroups(args.User, component.HealGroups);
            }
            else
            {
                groupsHeal = _damageable.CreateHealFromGroups(args.User, component.HealGroups);
            }
        }

        if (heal == null && groupsHeal != null)
            heal = groupsHeal;
        else if (heal != null && groupsHeal != null)
            heal += groupsHeal;

        if (heal == null || heal.Empty)
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (!_entityWhitelist.IsWhitelistPass(component.Whitelist, hitEnt))
                continue;

            if (_entityWhitelist.IsWhitelistPass(component.Blacklist, hitEnt))
                continue;

            if (_mobState.IsDead(hitEnt))
                continue;

            _damageable.TryChangeDamage(args.User, heal, true, false, origin: args.Weapon);
        }
    }
}
