using Content.Server.Atmos.EntitySystems;
using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed partial class ProjectilePressureSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;

    private const float NORMALPRESSURE = 101.325f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectilePressureComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectilePressureComponent component, ref ProjectileHitEvent ev)
    {
        var mixture = _atmos.GetContainingMixture(entity, true, true);
        if (mixture == null)
            return;

        var currentPressure = mixture.Pressure;

        float pressureModifier;
        if (currentPressure <= NORMALPRESSURE)
        {
            var normalized = currentPressure / NORMALPRESSURE;
            pressureModifier = 1 + (component.DamageMultiplier - 1) * (1 - normalized);
        }
        else
        {
            var pressureRatio = currentPressure / NORMALPRESSURE;
            pressureModifier = Math.Max(0.5f, 2 - pressureRatio);
        }

        
        var bonus = component.Ignore 
            ? ev.Damage * component.DamageMultiplier 
            : ev.Damage * (pressureModifier - 1);
        ev.Damage += bonus;
    }
}
