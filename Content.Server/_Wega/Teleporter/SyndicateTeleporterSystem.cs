/*using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server._Wega.Teleporter;
using Robust.Server.GameObjects;
using Content.Shared.GameTicking;
using Content.Shared.Body.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;
using Robust.Shared.Maths;

namespace Content.Server._Wega.Teleporter;

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId TeleportEffectPrototype = "TeleportEffect";

    public override void Initialize()
    {
        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUseInHand);
    }
private void OnUseInHand(Entity<SyndicateTeleporterComponent> teleporter, ref UseInHandEvent e)
{
    if (e.Handled)
        return;

    var transform = Transform(e.User);
    var direction = transform.LocalRotation.ToWorldVec().Normalized();

    List<EntityCoordinates> safeCoordinates = [];

    for (var i = 0; i < teleporter.Comp.TeleportationRangeLength; i++)
    {
        var offset = (teleporter.Comp.TeleportationRangeStart + i) * direction;
        var coordinates = transform.Coordinates.Offset(offset).SnapToGrid(EntityManager, _map);
        var tile = _mapSystem.GetTileRef(coordinates, _map);

        if (tile == null || _turf.IsTileBlocked(tile.Value, teleporter.Comp.CollisionGroup))
            continue;

        safeCoordinates.Add(coordinates);
    }

    EntityCoordinates resultCoordinates;

    if (safeCoordinates.Count > 0)
    {
        resultCoordinates = _random.Pick(safeCoordinates);
    }
    else
    {
        var offset = (teleporter.Comp.TeleportationRangeStart + _random.Next((int)teleporter.Comp.TeleportationRangeLength)) * direction;
        resultCoordinates = transform.Coordinates.Offset(offset);
    }

    Spawn(TeleportEffectPrototype, transform.Coordinates);
    Spawn(TeleportEffectPrototype, resultCoordinates);

    _transform.SetCoordinates(e.User, resultCoordinates);

        if (safeCoordinates.Count < 1)
            _body.GibBody(e.User, true);
}
}
