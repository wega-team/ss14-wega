using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.EnergyShield;

public sealed partial class EnergyShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeWeapon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyShieldOwnerComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<EnergyShieldOwnerComponent, ProjectileReflectAttemptEvent>(OnProjectileAttemptE);
    }

    private void OnAttacked(Entity<EnergyShieldOwnerComponent> ent, ref AttackedEvent args)
    {
        if (ent.Comp.ShieldEntity == null || ent.Comp.SustainingCount <= 0)
        {
            RemCompDeferred(ent.Owner, ent.Comp);
            QueueDel(ent.Comp.ShieldEntity);
            return;
        }

        ent.Comp.SustainingCount--;
        var damage = _meleeWeapon.GetDamage(args.Used, args.User);
        args.BonusDamage = -damage;

        if (ent.Comp.SustainingCount <= 0)
        {
            QueueDel(ent.Comp.ShieldEntity);
            RemCompDeferred(ent.Owner, ent.Comp);
        }
    }

    private void OnProjectileAttemptE(Entity<EnergyShieldOwnerComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (ent.Comp.ShieldEntity == null || ent.Comp.SustainingCount <= 0)
        {
            RemCompDeferred(ent.Owner, ent.Comp);
            QueueDel(ent.Comp.ShieldEntity);
            return;
        }

        ent.Comp.SustainingCount--;
        args.Cancelled = true;
        QueueDel(args.ProjUid);

        if (ent.Comp.SustainingCount <= 0)
        {
            QueueDel(ent.Comp.ShieldEntity);
            RemCompDeferred(ent.Owner, ent.Comp);
        }
    }
}
