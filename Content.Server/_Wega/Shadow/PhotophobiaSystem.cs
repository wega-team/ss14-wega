using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.Shadow.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Physics;
using Robust.Server.Containers;
using Content.Shared.Inventory;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;

namespace Content.Server.Shadow;

public sealed class PhotophobiaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    [ValidatePrototypeId<DamageTypePrototype>]
    private const string Damage = "Heat";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotophobiaComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PhotophobiaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime < comp.NextTickTime)
                continue;

            comp.NextTickTime = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.Interval);

            var lights = GetNearbyLights(uid);
            if (lights.Count == 0)
            {
                if (HasComp<ShadowWeaknessComponent>(uid))
                    RemComp<ShadowWeaknessComponent>(uid);
                continue;
            }

            if (comp.ApplyShadowWeakness && !HasComp<ShadowWeaknessComponent>(uid))
            {
                EnsureComp<ShadowWeaknessComponent>(uid, out var shadowWeakness);
                shadowWeakness.DamageModfier = comp.DamageModfier;
                shadowWeakness.SpeedModifier = comp.SpeedModifier;
            }

            if (comp.DamagePerLight != 0)
            {
                var damage = new DamageSpecifier { DamageDict = { { Damage, comp.DamagePerLight * lights.Count } } };
                _audio.PlayPvs(comp.DamageSound, uid, AudioParams.Default.WithVolume(-10f).WithPitchScale(1.15f).WithMaxDistance(2f));
                _damageable.TryChangeDamage(uid, damage, false, false);
            }

        }
    }

    private void OnUnpaused(EntityUid uid, PhotophobiaComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextTickTime += args.PausedTime;
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

            if (light.Radius < 2f)
                continue;

            if (TryComp<VisibilityComponent>(lightUid, out var visibility) && visibility.Layer != 1)
                continue;

            if (TryCheckContainingLight(lightUid))
            {
                continue;
            }

            if (IsTargetInsideCone(uid, lightUid))
            {
                continue;
            }


            float lightFactor = light.Energy * 0.5f + light.Radius - 2f;

            if (lightFactor > light.Radius)
                lightFactor = light.Radius - 0.5f;

            var lightXform = Transform(lightUid);
            var lightPos = _transform.GetWorldPosition(lightXform);
            var distanceSq = (worldPos - lightPos).LengthSquared();

            if (distanceSq > lightFactor * lightFactor)
            {
                continue;
            }

            if (GetOccluded(uid, lightUid))
            {
                result.Add(lightUid);
            }
        }

        return result;
    }

    private bool TryCheckContainingLight(EntityUid uid)
    {
        if (!_container.TryGetContainingContainer(uid, out var container))
            return false;

        if (_inventorySystem.TryGetContainingSlot(uid, out _))
            return false;

        if (TryCheckHandLight(container.Owner, uid))
            return false;

        return true;
    }

    private bool TryCheckHandLight(Entity<HandsComponent?> user, EntityUid uid)
    {
        if (Resolve(user.Owner, ref user.Comp, false))
        {
            foreach (var held in _handsSystem.EnumerateHeld(user))
            {
                if (held == uid)
                    return true;
            }
        }

        return false;
    }

    public bool GetOccluded(EntityUid sourceUid, EntityUid lightUid)
    {
        if (sourceUid == lightUid)
            return true;

        if (_container.TryGetContainingContainer(lightUid, out var container) && container.Owner == sourceUid)
            return true;

        var sourceXform = Transform(sourceUid);
        var lightXform = Transform(lightUid);

        if (sourceXform.MapID != lightXform.MapID)
            return false;

        var sourcePos = _transform.GetWorldPosition(sourceXform);
        var lightPos = _transform.GetWorldPosition(lightXform);
        var direction = lightPos - sourcePos;
        var distance = direction.Length();

        if (distance <= 0.1f)
            return false;

        var normalizedDir = direction.Normalized();
        var ray = new CollisionRay(sourcePos, normalizedDir, (int)(CollisionGroup.WallLayer | CollisionGroup.AirlockLayer));
        var rayCastResults = _physics.IntersectRay(sourceXform.MapID, ray, distance, sourceUid, false);

        foreach (var result in rayCastResults)
        {
            if (TryComp<OccluderComponent>(result.HitEntity, out var occluder) && occluder.Enabled)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsTargetInsideCone(EntityUid targetUid, EntityUid lightUid)
    {
        if (!TryComp<PointLightComponent>(lightUid, out var lightComp))
            return true;

        // TODO: Избавится от этого ужаса.
        if (lightComp.MaskPath == "/Textures/Effects/LightMasks/double_cone.png")
            return true;

        if (lightComp.MaskPath == null)
            return false;

        var tXform = Transform(targetUid);
        var lXform = Transform(lightUid);
        var tPos = _transform.GetWorldPosition(tXform);
        var lPos = _transform.GetWorldPosition(lXform);

        float toX = tPos.X - lPos.X;
        float toY = tPos.Y - lPos.Y;

        float dist = MathF.Sqrt(toX * toX + toY * toY);

        float invDist = 1f / dist;
        float toNx = toX * invDist;
        float toNy = toY * invDist;

        float ang = (float)_transform.GetWorldRotation(lXform).Theta;

        ang -= MathF.PI / 2f;

        float fX = MathF.Cos(ang);
        float fY = MathF.Sin(ang);

        float dot = fX * toNx + fY * toNy;

        const float coneAngleDeg = 90f;
        float cosHalf = MathF.Cos(MathF.PI * coneAngleDeg / 360f);

        return dot <= cosHalf;
    }
}
