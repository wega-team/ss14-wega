using System.Numerics;
using Content.Server.Lavaland.Components;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Tiles;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;

namespace Content.Server.Lavaland.Systems;

public sealed partial class AuxiliaryBaseLandingSystem : EntitySystem
{
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private RadioSystem _radio = default!;

    private Dictionary<EntityUid, MapId> _gridToTempMap = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AuxiliaryBaseLandingComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<AuxiliaryBaseLandingComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
    }

    private void OnUse(Entity<AuxiliaryBaseLandingComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        if (_useDelay.IsDelayed(ent.Owner))
            return;

        var userTransform = Transform(args.User);
        if (!HasComp<LavalandComponent>(userTransform.MapUid) || userTransform.MapUid != userTransform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("auxiliary-base-landing-failed"), args.User, args.User);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        _mapSystem.CreateMap(out var tempMapId);
        if (!_loader.TryLoadGrid(tempMapId, ent.Comp.BasePath, out var grid, offset: Vector2.Zero))
        {
            _popup.PopupEntity(Loc.GetString("auxiliary-base-landing-failed-load"), args.User, args.User);
            _mapSystem.DeleteMap(tempMapId);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        var gridUid = grid.Value.Owner;
        var gridComp = grid.Value.Comp;

        var targetCoords = userTransform.Coordinates;
        var worldAABB = new Box2Rotated(
            gridComp.LocalAABB.Translated(targetCoords.Position),
            Angle.Zero
        );

        var walls = _lookup.GetEntitiesIntersecting(userTransform.MapUid.Value, worldAABB, LookupFlags.Static);
        if (walls.Count > 0)
        {
            Del(gridUid);
            _mapSystem.DeleteMap(tempMapId);
            var boxSize = $"({gridComp.LocalAABB.Width:F1}x{gridComp.LocalAABB.Height:F1})";
            _popup.PopupEntity(Loc.GetString("auxiliary-base-landing-failed-box", ("box", boxSize)), args.User, args.User);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        if (!TryComp<ShuttleComponent>(gridUid, out var shuttleComp))
            shuttleComp = EnsureComp<ShuttleComponent>(gridUid);

        _shuttle.Enable(gridUid);
        _gridToTempMap[gridUid] = tempMapId;

        var targetAngle = Angle.Zero;
        _shuttle.FTLToCoordinates(gridUid, shuttleComp, targetCoords, targetAngle);
        _popup.PopupEntity(Loc.GetString("auxiliary-base-landing-success"), args.User, args.User);

        var navMap = EnsureComp<NavMapComponent>(gridUid);
        _navMap.RefreshGridWithOffset(gridUid, navMap, gridComp, targetCoords.Position);
        EnsureComp<GridLavalandWeatherProtectionComponent>(gridUid);
        EnsureComp<ProtectedGridComponent>(gridUid);

        SendLandingAnnounce(userTransform.MapUid.Value, targetCoords);

        QueueDel(ent);
    }

    private void OnFTLStarted(ref FTLStartedEvent args)
    {
        var grid = args.Entity;
        if (_gridToTempMap.TryGetValue(grid, out var tempMapId))
        {
            if (_mapSystem.MapExists(tempMapId))
                _mapSystem.DeleteMap(tempMapId);

            _gridToTempMap.Remove(grid);
        }
    }

    private void OnComponentShutdown(Entity<AuxiliaryBaseLandingComponent> ent, ref ComponentShutdown args)
    {
        var toRemove = new List<EntityUid>();
        foreach (var (grid, mapId) in _gridToTempMap)
        {
            if (!Exists(grid))
            {
                if (_mapSystem.MapExists(mapId))
                    _mapSystem.DeleteMap(mapId);

                toRemove.Add(grid);
            }
        }

        foreach (var grid in toRemove)
        {
            _gridToTempMap.Remove(grid);
        }
    }

    private void SendLandingAnnounce(EntityUid planet, EntityCoordinates targetCoords)
    {
        Entity<LavalandAvanpostComponent>? sender = null;
        var query = EntityQueryEnumerator<LavalandAvanpostComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var transform))
        {
            if (transform.MapUid != planet)
                continue;

            sender = (uid, comp);
            break;
        }

        if (sender == null)
            return;

        var x = (int)targetCoords.X;
        var y = (int)targetCoords.Y;

        var message = Loc.GetString("auxiliary-base-landing-announce", ("coords", $"({x}, {y})"));
        _radio.SendRadioMessage(sender.Value.Owner, message, sender.Value.Comp.AnnouncementChannel,
            sender.Value.Owner, escapeMarkup: false);
    }
}
