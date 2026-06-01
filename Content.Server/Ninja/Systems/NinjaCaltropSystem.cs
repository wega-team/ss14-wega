using Content.Server.Ninja.Components;
using Content.Shared.Maps;
using Content.Shared.Ninja.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Spawners;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the Electro-Caltrop ability: places a 3-tile-wide row of caltrop traps
/// behind the ninja. Each caltrop deals 10 piercing damage and knocks down the
/// tripper for 2 seconds. The ninja who deployed them is immune to their own caltrops.
/// </summary>
public sealed class NinjaCaltropSystem : EntitySystem
{
    private const float CaltropCharge = 100f;
    private const float CaltropLifetime = 10f;

    [Dependency] private readonly NinjaCloakSystem _cloak      = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja     = default!;
    [Dependency] private readonly SharedMapSystem  _mapSystem  = default!;
    [Dependency] private readonly TurfSystem       _turf       = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent,   NinjaCaltropEvent>(OnCaltrop);
        SubscribeLocalEvent<NinjaCaltropComponent, StepTriggerAttemptEvent>(OnStepAttempt);
    }

    private void OnCaltrop(Entity<NinjaSuitComponent> ent, ref NinjaCaltropEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (!_ninja.TryUseCharge(user, CaltropCharge))
            return;

        var xform = Transform(user);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        foreach (var pos in GetBehindPositions(xform, gridUid, mapGrid))
        {
            var caltrop = Spawn("NinjaCaltrop", pos);
            EnsureComp<NinjaCaltropComponent>(caltrop).Caster = user;
            EnsureComp<TimedDespawnComponent>(caltrop).Lifetime = CaltropLifetime;
        }
    }

    /// <summary>
    /// Returns 3 tile-snapped positions forming a perpendicular row 1 tile behind the ninja,
    /// mirroring the mage force-wall pattern.
    /// </summary>
    private List<EntityCoordinates> GetBehindPositions(TransformComponent xform, EntityUid gridUid, MapGridComponent mapGrid)
    {
        var behindPos = xform.Coordinates.Offset(-xform.LocalRotation.ToWorldVec().Normalized());

        if (!_turf.TryGetTileRef(behindPos, out var tileRef))
            return new List<EntityCoordinates>();

        var idx = tileRef.Value.GridIndices;
        var center = _mapSystem.GridTileToLocal(gridUid, mapGrid, idx);

        var dir = xform.LocalRotation.GetCardinalDir();
        EntityCoordinates left, right;

        switch (dir)
        {
            case Direction.North:
            case Direction.South:
                left  = _mapSystem.GridTileToLocal(gridUid, mapGrid, idx + (-1, 0));
                right = _mapSystem.GridTileToLocal(gridUid, mapGrid, idx + ( 1, 0));
                break;
            case Direction.East:
            case Direction.West:
                left  = _mapSystem.GridTileToLocal(gridUid, mapGrid, idx + (0, -1));
                right = _mapSystem.GridTileToLocal(gridUid, mapGrid, idx + (0,  1));
                break;
            default:
                return new List<EntityCoordinates> { center };
        }

        return new List<EntityCoordinates> { left, center, right };
    }

    private void OnStepAttempt(Entity<NinjaCaltropComponent> ent, ref StepTriggerAttemptEvent args)
    {
        // The ninja who placed the caltrops is immune to them
        if (args.Tripper == ent.Comp.Caster)
            return;

        args.Continue = true;
    }
}
