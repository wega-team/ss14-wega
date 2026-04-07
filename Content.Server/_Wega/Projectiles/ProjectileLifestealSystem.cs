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
        var totalDamage = _damage.GetTotalDamage(shooter);
        if (totalDamage == 0)
            return;

        var healAmount = FixedPoint2.Min(component.StealAmount, totalDamage);
        if (healAmount <= 0)
            return;

        _damage.HealDistributed(shooter, healAmount);
    }
}
