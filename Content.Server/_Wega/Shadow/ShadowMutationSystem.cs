using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.Shadow.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Physics;
using Content.Shared.Interaction;

namespace Content.Shared.Lighting;

public sealed class ShadowMutationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string Damage = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowMutateComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadowMutateComponent>();
        while (query.MoveNext(out var uid, out var mutate))
        {
            if (_gameTiming.CurTime < mutate.NextDamageTime)
                continue;

            mutate.NextDamageTime = _gameTiming.CurTime + TimeSpan.FromSeconds(mutate.DamageInterval);

            var lights = GetNearbyLights(uid);
            if (lights.Count == 0)
                continue;

            var damage = new DamageSpecifier { DamageDict = { { Damage, mutate.DamagePerLight * lights.Count } } };
            _damageable.TryChangeDamage(uid, damage);
        }
    }

    private void OnUnpaused(EntityUid uid, ShadowMutateComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextDamageTime += args.PausedTime;
    }

    private List<EntityUid> GetNearbyLights(EntityUid uid)
    {
        var result = new List<EntityUid>();
        var xform = Transform(uid);
        var worldPos = _transform.GetWorldPosition(xform);

        foreach (var (lightUid, light) in _lookup.GetEntitiesInRange<PointLightComponent>(xform.Coordinates, 10f))
        {
            if (!light.Enabled)
                continue;

            var lightXform = Transform(lightUid);
            var lightPos = _transform.GetWorldPosition(lightXform);
            var distanceSq = (worldPos - lightPos).LengthSquared();

            if (distanceSq > light.Radius * light.Radius)
                continue;

            if (_interaction.InRangeUnobstructed(uid, lightUid, range: 1000f,
                collisionMask: CollisionGroup.Impassable, popup: false))
                result.Add(lightUid);
        }

        return result;
    }
}
