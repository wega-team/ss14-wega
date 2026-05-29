using Content.Server.Disease;
using Content.Shared.Projectiles;
using Robust.Shared.Random;

namespace Content.Server.Projectiles;

public sealed class ProjectileInfectSystem : EntitySystem
{
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileInfectComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileInfectComponent component, ref ProjectileHitEvent ev)
    {
        var shooter = ev.Shooter;
        if (shooter == null || shooter == ev.Target)
            return;

        if (!_random.Prob(component.Prob))
            return;

        _disease.TryAddDisease(ev.Target, component.Infection);
    }
}
