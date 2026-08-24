using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Upgrades.Components;

namespace Content.Shared.Weapons.Ranged.Upgrades;

public sealed partial class GunUpgradeSystem
{
    private const float PelletAoEMultiplier = 0.125f;

    [SubscribeLocalEvent]
    private void OnUpgradedAmmoShot(Entity<UpgradeableGunComponent> ent, ref AmmoShotEvent args)
    {
        var upgrades = GetCurrentUpgrades(ent);
        if (upgrades.Count == 0)
            return;

        var pellets = 1;

        foreach (var fired in args.FiredProjectiles)
        {
            var carrier = HasComp<AmmoComponent>(fired);

            if (carrier)
                pellets = TryComp<ProjectileSpreadComponent>(fired, out var spread) && spread.Count > 1
                    ? spread.Count
                    : 1;

            if (!TryComp<ProjectileComponent>(fired, out var projectile))
                continue;

            foreach (var upgrade in upgrades)
            {
                if (TryComp<GunUpgradeDamageComponent>(upgrade, out var damage))
                {
                    if (carrier)
                        projectile.Damage -= damage.Damage * ((pellets - 1) / (float) pellets);
                    else
                        projectile.Damage += damage.Damage / (float) pellets;
                }

                if (pellets > 1 && HasComp<GunUpgradeAoEComponent>(upgrade))
                    EnsureComp<ProjectileAoEComponent>(fired).DamageMultiplier = PelletAoEMultiplier;

                if (carrier)
                    continue;

                if (TryComp<GunUpgradeLifestealComponent>(upgrade, out var lifesteal))
                    EnsureComp<ProjectileLifestealComponent>(fired).StealAmount = lifesteal.StealAmount / pellets;

                if (HasComp<GunUpgradePressureComponent>(upgrade) && TryComp<ProjectilePressureComponent>(fired, out var pressure))
                    pressure.Ignore = true;
            }
        }
    }
}
