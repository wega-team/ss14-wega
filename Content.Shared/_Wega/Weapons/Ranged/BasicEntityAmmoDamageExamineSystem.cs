using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed partial class BasicEntityAmmoDamageExamineSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DamageExamineSystem _damageExamine = default!;
    [Dependency] private IComponentFactory _factory = default!;

    [SubscribeLocalEvent]
    private void OnDamageExamine(Entity<BasicEntityAmmoProviderComponent> ent, ref DamageExamineEvent args)
    {
        if (!ProtoMan.TryIndex<EntityPrototype>(ent.Comp.Proto, out var proto))
            return;

        if (!proto.TryComp<ProjectileComponent>(out var projectile, _factory) || projectile.Damage.Empty)
            return;

        var damage = _damageable.ApplyUniversalAllModifiers(projectile.Damage * _damageable.UniversalProjectileDamageModifier);
        _damageExamine.AddDamageExamine(args.Message, damage, Loc.GetString("damage-projectile"));
    }
}
