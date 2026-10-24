using System.Collections.Concurrent;
using System.Numerics;
using Content.Shared.Audio;
using Content.Shared.Audio.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Audio;

/// <summary>
/// Traces sound rays to calculate acoustic insulation and "generate" visual paths.
/// </summary>
public sealed partial class SoundRayTracer : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    // Collision and raycast precision settings
    private const int SoundOpaqueMask = (int)CollisionGroup.SoundOpaque;
    private const float RayEpsilon = 0.1f;
    private const float MinLenSq = 1e-6f;

    // Path limits and absorption
    private const int MaxBounces = 10;
    private const float MaxTotalDistance = 50f;
    private const float DistanceAbsorptionPerMeter = 0.02f;

    // Cache settings and storage
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan CacheCleanupTtl = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan InsulatorCacheTtl = TimeSpan.FromSeconds(30);

    private TimeSpan _nextCleanup;

    // Thread-safe caches because audio processing runs on parallel workers
    private readonly ConcurrentDictionary<(EntityUid Source, EntityUid Listener, MapId Map), (SoundTraceResult Result, TimeSpan Time)> _cache = new();
    private readonly ConcurrentDictionary<EntityUid, (bool IsOn, TimeSpan Time)> _insulatorCache = new();

    /// <summary>
    /// Periodically cleans up expired entries from both trace and insulator caches.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextCleanup)
            return;

        _nextCleanup = now + TimeSpan.FromSeconds(5);

        foreach (var kvp in _cache)
        {
            if (now - kvp.Value.Time > CacheCleanupTtl)
                _cache.TryRemove(kvp.Key, out _);
        }

        foreach (var kvp in _insulatorCache)
        {
            if (now - kvp.Value.Time > InsulatorCacheTtl)
                _insulatorCache.TryRemove(kvp.Key, out _);
        }
    }

    /// <summary>
    /// Returns the trace result from cache or calculates a new one.
    /// </summary>
    public SoundTraceResult Trace(EntityUid source, EntityUid listener, MapId mapId)
    {
        var key = (source, listener, mapId);
        var now = _timing.CurTime;

        if (_cache.TryGetValue(key, out var cached) && now - cached.Time < CacheTtl)
            return cached.Result;

        var result = TraceInternal(source, listener, mapId);
        _cache[key] = (result, now);
        return result;
    }

    /// <summary>
    /// Prepares the initial state and starts the recursive trace.
    /// </summary>
    private SoundTraceResult TraceInternal(EntityUid source, EntityUid listener, MapId mapId)
    {
        var sourceXform = Transform(source);
        var sourcePos = _transform.GetWorldPosition(sourceXform);
        var listenerPos = _transform.GetWorldPosition(listener);
        var diff = listenerPos - sourcePos;

        if (diff.LengthSquared() < MinLenSq)
        {
            var immediatePath = new List<SoundRaySegment>(1);
            immediatePath.Add(new SoundRaySegment(sourcePos, listenerPos, 0f));
            return new SoundTraceResult(0f, true, immediatePath);
        }

        var initialDir = diff / diff.Length();
        var visited = new HashSet<EntityUid> { source };

        if (sourceXform.ParentUid.IsValid())
            visited.Add(sourceXform.ParentUid);

        if (IsSourceOnInsulator(source, sourcePos, mapId))
        {
            var nearbyInsulators = _lookup.GetEntitiesInRange<SoundInsulationComponent>(
                new MapCoordinates(sourcePos, mapId),
                1.5f,
                LookupFlags.Static);

            foreach (var ent in nearbyInsulators)
            {
                if (visited.Contains(ent.Owner))
                    continue;

                var entPos = _transform.GetWorldPosition(ent.Owner);
                var delta = entPos - sourcePos;

                if (MathF.Abs(delta.X) + MathF.Abs(delta.Y) <= 1.5f)
                    visited.Add(ent.Owner);
            }
        }

        var path = new List<SoundRaySegment>();
        var state = new RayPredicateState(source, listener, visited, EntityManager);

        return TraceRecursive(sourcePos, listenerPos, initialDir, mapId, 0, 0f, 0f, visited, path, state);
    }

    /// <summary>
    /// Checks if the source is placed directly on a sound insulator.
    /// Uses a cache because wall-mounted objects are static and rarely move.
    /// </summary>
    private bool IsSourceOnInsulator(EntityUid source, Vector2 sourcePos, MapId mapId)
    {
        var now = _timing.CurTime;
        if (_insulatorCache.TryGetValue(source, out var cached) && now - cached.Time < InsulatorCacheTtl)
            return cached.IsOn;

        var nearby = _lookup.GetEntitiesInRange<SoundInsulationComponent>(
            new MapCoordinates(sourcePos, mapId),
            0.05f,
            LookupFlags.Static);

        var isOn = false;
        foreach (var ent in nearby)
        {
            if (ent.Owner != source)
            {
                isOn = true;
                break;
            }
        }

        _insulatorCache[source] = (isOn, now);
        return isOn;
    }

    /// <summary>
    /// Performs the recursive ray tracing logic.
    /// Handles reflections, transmission, and listener hits while avoiding heap allocations.
    /// </summary>
    private SoundTraceResult TraceRecursive(
        Vector2 origin,
        Vector2 listener,
        Vector2 direction,
        MapId mapId,
        int bounce,
        float accumulatedAbsorption,
        float distanceTraveled,
        HashSet<EntityUid> visited,
        List<SoundRaySegment> path,
        RayPredicateState state)
    {
        if (bounce > MaxBounces || distanceTraveled >= MaxTotalDistance)
            return new SoundTraceResult(accumulatedAbsorption, false, new List<SoundRaySegment>(path));

        var dirLenSq = direction.LengthSquared();
        if (dirLenSq < MinLenSq)
            return new SoundTraceResult(accumulatedAbsorption, false, new List<SoundRaySegment>(path));

        if (MathF.Abs(dirLenSq - 1f) > 1e-4f)
            direction /= MathF.Sqrt(dirLenSq);

        var toListener = listener - origin;
        var listenerDist = toListener.Length();
        var toListenerNorm = listenerDist > 0.001f ? toListener / listenerDist : Vector2.Zero;

        var isAimingAtListener = Vector2.Dot(direction, toListenerNorm) > 0.95f;
        var maxRemainingDistance = MaxTotalDistance - distanceTraveled;

        var castLength = MathF.Min(maxRemainingDistance, isAimingAtListener ? listenerDist + RayEpsilon : maxRemainingDistance);

        var castRay = new CollisionRay(origin, direction, SoundOpaqueMask);
        RayCastResults? hit = null;

        foreach (var result in _physics.IntersectRayWithPredicate(mapId, castRay, state, TracePredicate, castLength))
        {
            hit = result;
            break;
        }

        if (!hit.HasValue)
        {
            if (isAimingAtListener && castLength >= listenerDist)
            {
                var listenerAbs = Math.Clamp(accumulatedAbsorption + listenerDist * DistanceAbsorptionPerMeter, 0f, 1f);
                path.Add(new SoundRaySegment(origin, listener, listenerAbs));
                return new SoundTraceResult(listenerAbs, true, new List<SoundRaySegment>(path));
            }

            var endPos = origin + direction * castLength;
            var finalAbs = Math.Clamp(accumulatedAbsorption + castLength * DistanceAbsorptionPerMeter, 0f, 1f);
            path.Add(new SoundRaySegment(origin, endPos, finalAbs));
            return new SoundTraceResult(finalAbs, false, new List<SoundRaySegment>(path));
        }

        var hitPos = hit.Value.HitPos;
        var hitEntity = hit.Value.HitEntity;
        var toHit = hitPos - origin;
        var hitDist = toHit.Length();

        if (hitEntity == state.Listener || (isAimingAtListener && hitDist > listenerDist))
        {
            var finalAbs = Math.Clamp(accumulatedAbsorption + listenerDist * DistanceAbsorptionPerMeter, 0f, 1f);
            path.Add(new SoundRaySegment(origin, listener, finalAbs));
            return new SoundTraceResult(finalAbs, true, new List<SoundRaySegment>(path));
        }

        if (hitDist < RayEpsilon)
        {
            visited.Add(hitEntity);
            var res = TraceRecursive(origin, listener, direction, mapId, bounce, accumulatedAbsorption, distanceTraveled, visited, path, state);
            visited.Remove(hitEntity);
            return res;
        }

        if (!TryComp<SoundInsulationComponent>(hitEntity, out var insulationComp))
        {
            var finalAbs = Math.Clamp(accumulatedAbsorption + hitDist * DistanceAbsorptionPerMeter, 0f, 1f);
            path.Add(new SoundRaySegment(origin, hitPos, finalAbs));
            return new SoundTraceResult(finalAbs, false, new List<SoundRaySegment>(path));
        }

        var distAbs = hitDist * DistanceAbsorptionPerMeter;
        var newAbs = accumulatedAbsorption + insulationComp.AbsorptionFactor + distAbs;
        var newDistance = distanceTraveled + hitDist;

        path.Add(new SoundRaySegment(origin, hitPos, Math.Clamp(newAbs, 0f, 1f)));

        if (newDistance >= MaxTotalDistance)
            return new SoundTraceResult(newAbs, false, new List<SoundRaySegment>(path));

        if (insulationComp.BlocksTransmission)
        {
            var normal = MathF.Abs(direction.X) > MathF.Abs(direction.Y)
                ? new Vector2(-MathF.Sign(direction.X), 0)
                : new Vector2(0, -MathF.Sign(direction.Y));

            var reflectDir = Vector2.Reflect(direction, normal);
            if (Vector2.Dot(reflectDir, normal) < 0)
                reflectDir = -reflectDir;

            var reflectOrigin = hitPos + normal * RayEpsilon;

            var res = TraceRecursive(reflectOrigin, listener, reflectDir, mapId, bounce + 1, newAbs, newDistance, visited, path, state);
            if (res.ReachedListener)
                return res;

            return new SoundTraceResult(res.TotalAbsorption, false, new List<SoundRaySegment>(path));
        }
        else
        {
            var insideOrigin = hitPos + direction * RayEpsilon;
            var aimDiff = listener - insideOrigin;

            Vector2 aimDir;
            if (aimDiff.LengthSquared() < MinLenSq)
                aimDir = direction;
            else
                aimDir = aimDiff / aimDiff.Length();

            visited.Add(hitEntity);

            var res = TraceRecursive(insideOrigin, listener, aimDir, mapId, bounce + 1, newAbs, newDistance, visited, path, state);
            visited.Remove(hitEntity);

            if (res.ReachedListener)
                return res;

            return new SoundTraceResult(res.TotalAbsorption, false, new List<SoundRaySegment>(path));
        }
    }

    /// <summary>
    /// Static predicate that prevents closure allocations during raycasts.
    /// Returns true if the entity should be ignored by the ray.
    /// </summary>
    private static bool TracePredicate(EntityUid ent, RayPredicateState state)
    {
        if (ent == state.Listener)
            return false;

        if (ent == state.Source)
            return true;

        if (state.Visited.Contains(ent))
            return true;

        if (!state.EntManager.TryGetComponent<SoundInsulationComponent>(ent, out var c))
            return true;

        return !c.Isolates;
    }

    /// <summary>
    /// Encapsulates the state required by the static raycast predicate.
    /// Used to prevent heap allocations from closures during frequent raycasts.
    /// </summary>
    private readonly record struct RayPredicateState(EntityUid Source, EntityUid Listener, HashSet<EntityUid> Visited, IEntityManager EntManager);
}
