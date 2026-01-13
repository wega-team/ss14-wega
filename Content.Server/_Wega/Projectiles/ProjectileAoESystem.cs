using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed class ProjectileAoESystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileAoEComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileAoEComponent component, ref ProjectileHitEvent ev)
    {
        var target = ev.Target;
        var ents = _lookup.GetEntitiesInRange<DamageableComponent>(Transform(entity).Coordinates, component.DamageRadius)
            .Where(e => e.Owner != target);

        foreach (var ent in ents)
        {
            _damage.TryChangeDamage(ent.Owner, ev.Damage * component.DamageMultiplier);
        }
    }
}
