using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;

namespace Content.Server.Projectiles;

public sealed partial class ProjectileSlowdownSystem : EntitySystem
{
    [Dependency] private MovementModStatusSystem _movementMod = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileSlowdownComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileSlowdownComponent component, ref ProjectileHitEvent ev)
    {
        if (ev.Shooter == null || ev.Shooter == ev.Target)
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(ev.Target, component.EffectProto, component.Duration, component.Multiplier);
    }
}
