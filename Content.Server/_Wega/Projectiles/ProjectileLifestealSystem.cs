using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed class ProjectileLifestealSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileLifestealComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileLifestealComponent component, ref ProjectileHitEvent ev)
    {
        var target = ev.Target;
        if (!HasComp<MobStateComponent>(target) || _mobState.IsDead(target) || ev.Shooter == null)
            return;

        var shooter = ev.Shooter.Value;
        if (!TryComp<DamageableComponent>(shooter, out var damageable) || damageable.TotalDamage == 0)
            return;

        var totalDamage = damageable.TotalDamage;
        var healAmount = FixedPoint2.Min(component.StealAmount, totalDamage);
        if (healAmount <= 0)
            return;

        var toHeal = new DamageSpecifier();

        var remainingHeal = healAmount;
        var sortedDamages = damageable.Damage.DamageDict.Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value).ToList();

        foreach (var (damageType, currentDamage) in sortedDamages)
        {
            if (remainingHeal <= 0)
                break;

            var healForThisType = FixedPoint2.Min(currentDamage, remainingHeal);

            toHeal.DamageDict[damageType] = -healForThisType;
            remainingHeal -= healForThisType;
        }

        if (toHeal.DamageDict.Count > 0)
        {
            _damage.TryChangeDamage(shooter, toHeal, true, false);
        }
    }
}
