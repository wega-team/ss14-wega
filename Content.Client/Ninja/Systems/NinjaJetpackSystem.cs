using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Ninja.Systems;

public sealed class NinjaJetpackSystem : SharedNinjaJetpackSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    // Never predict enabling on client — component arrives from server state only,
    // same as the standard JetpackSystem. This prevents prediction spam.
    protected override bool CanEnable(EntityUid uid, NinjaJetpackComponent comp) => false;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<ActiveNinjaJetpackComponent, NinjaJetpackComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var active, out var comp, out var xform))
        {
            // Guard against invalid LastCoordinates on first frame (entity ID 0 logs errors in InRange).
            if (active.LastCoordinates.EntityId.IsValid() &&
                _transform.InRange(xform.Coordinates, active.LastCoordinates, active.MaxDistance))
            {
                if (_timing.CurTime < active.TargetTime)
                    continue;
            }

            active.LastCoordinates = _transform.GetMoverCoordinates(xform.Coordinates);
            active.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(active.EffectCooldown);

            CreateParticles(uid, comp);
        }
    }

    private void CreateParticles(EntityUid suitUid, NinjaJetpackComponent comp)
    {
        var xform = Transform(suitUid);

        if (comp.JetpackUser == null)
            return;

        if (TryComp<PhysicsComponent>(comp.JetpackUser.Value, out var body) &&
            body.LinearVelocity.LengthSquared() < 1f)
        {
            return;
        }

        var coordinates = xform.Coordinates;
        var gridUid = _transform.GetGrid(coordinates);

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(gridUid.Value,
                _mapSystem.WorldToLocal(gridUid.Value, grid, _transform.ToMapCoordinates(coordinates).Position));
        }
        else if (xform.MapUid != null)
        {
            coordinates = new EntityCoordinates(xform.MapUid.Value, _transform.GetWorldPosition(xform));
        }
        else
        {
            return;
        }

        // SpiderOSComponent is on the suit entity, not the user.
        var effectProto = GetEffectPrototype(suitUid);
        Spawn(effectProto, coordinates);
    }

    private string GetEffectPrototype(EntityUid suitUid)
    {
        if (!TryComp<SpiderOSComponent>(suitUid, out var spiderOS))
            return "NinjaJetpackEffectGreen";

        return spiderOS.SuitColor switch
        {
            0 => "NinjaJetpackEffectRed",
            1 => "NinjaJetpackEffectBlue",
            _ => "NinjaJetpackEffectGreen",
        };
    }
}
