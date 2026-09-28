using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Feroxi;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Feroxi;

public sealed partial class BloodSenseScentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BloodSenseSystem _bloodSense = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<BloodstreamComponent>> _candidates = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodSenseComponent, BloodSenseActiveComponent>();
        while (query.MoveNext(out var uid, out var sense, out var active))
        {
            if (_timing.CurTime < active.NextUpdate)
                continue;

            active.NextUpdate = _timing.CurTime + sense.UpdateInterval;

            if (!_mobState.IsAlive(uid))
            {
                TurnOff(uid, sense, null);
                continue;
            }

            if (_bloodSense.IsTooThirsty((uid, sense)))
            {
                TurnOff(uid, sense, "blood-sense-too-thirsty");
                continue;
            }

            if (TryComp<SatiationComponent>(uid, out var satiation))
                _satiation.ModifyValue((uid, satiation), SatiationSystem.Thirst, -sense.ThirstCost * (float) sense.UpdateInterval.TotalSeconds);

            active.Targets.Clear();

            if (CanSmell(uid, sense))
                FindTargets(uid, sense, active);

            Dirty(uid, active);
        }
    }

    private void TurnOff(EntityUid uid, BloodSenseComponent sense, string? popup)
    {
        RemCompDeferred<BloodSenseActiveComponent>(uid);
        _actions.SetToggled(sense.ActionEntity, false);

        if (popup != null)
            _popup.PopupEntity(Loc.GetString(popup), uid, uid, PopupType.SmallCaution);
    }

    private bool CanSmell(EntityUid uid, BloodSenseComponent sense) // проверка на намордники
    {
        if (_internals.AreInternalsWorking(uid))
            return false;

        return HasAir(uid, sense);
    }

    private bool HasAir(EntityUid uid, BloodSenseComponent sense) // проверка на давление
    {
        var air = _atmosphere.GetTileMixture(uid);
        return air != null && air.Pressure >= sense.MinPressure;
    }

    private void FindTargets(EntityUid uid, BloodSenseComponent sense, BloodSenseActiveComponent active) // месячные
    {
        var origin = _transform.GetMapCoordinates(uid);

        _candidates.Clear();
        _lookup.GetEntitiesInRange(origin, sense.Radius, _candidates);

        foreach (var (target, bloodstream) in _candidates)
        {
            if (target == uid || bloodstream.BleedAmount <= 0f || _mobState.IsDead(target))
                continue;

            if (!HasAir(target, sense)) // проверка на то, что в месте, где стоит цель, есть дистра.
                continue;

            var targetPos = _transform.GetMapCoordinates(target);
            if (targetPos.MapId != origin.MapId)
                continue;

            var delta = targetPos.Position - origin.Position;
            var distance = delta.Length();
            if (distance > sense.Radius)
                continue;

            var walls = distance > 0.01f ? CountWalls(uid, origin.MapId, origin.Position, delta, distance) : 0;

            var strength = bloodstream.BleedAmount
                           * (1f - distance / sense.Radius)
                           * MathF.Pow(sense.WallFactor, walls);

            if (strength >= sense.Threshold)
                active.Targets[target] = strength;
        }
    }

    private int CountWalls(EntityUid uid, MapId mapId, Vector2 origin, Vector2 delta, float distance) // считаем стены
    {
        var ray = new CollisionRay(origin, delta / distance, (int) CollisionGroup.Impassable);
        var walls = 0;

        foreach (var hit in _physics.IntersectRay(mapId, ray, distance, uid, returnOnFirstHit: false))
        {
            if (TryComp<AirtightComponent>(hit.HitEntity, out var airtight) && airtight.AirBlocked)
                walls++;
        }

        return walls;
    }
}
