using System.Linq;
using Content.Server.Construction.Completions;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.NodeContainer;
using Content.Shared.Tools.Components;
using Content.Shared.VentCraw;
using Content.Shared.VentCraw.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.VentCraw;

public sealed class VentCrawTubeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly VentCrawableSystem _ventCrawableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawTubeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<VentCrawTubeComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<VentCrawTubeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VentCrawTubeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VentCrawTubeComponent, ConstructionBeforeDeleteEvent>(OnDeconstruct);
        SubscribeLocalEvent<VentCrawTubeComponent, AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<VentCrawTubeComponent, BreakageEventArgs>(OnBreak);

        SubscribeLocalEvent<VentCrawBendComponent, GetVentCrawsConnectableDirectionsEvent>(OnGetBendConnectableDirections);
        SubscribeLocalEvent<VentCrawEntryComponent, GetVentCrawsConnectableDirectionsEvent>(OnGetEntryConnectableDirections);
        SubscribeLocalEvent<VentCrawEntryComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbedVerb);
        SubscribeLocalEvent<VentCrawJunctionComponent, GetVentCrawsConnectableDirectionsEvent>(OnGetJunctionConnectableDirections);
        SubscribeLocalEvent<VentCrawManifoldComponent, GetVerbsEvent<Verb>>(OnGetManifoldVerbs);
        SubscribeLocalEvent<VentCrawTransitComponent, GetVentCrawsConnectableDirectionsEvent>(OnGetTransitConnectableDirections);
        SubscribeLocalEvent<VentCrawlerComponent, EnterVentDoAfterEvent>(OnDoAfterEnterTube);
    }

    public EntityUid? NextTubeFor(EntityUid target, Direction nextDirection, int pipeLayer = 0, VentCrawTubeComponent? targetTube = null)
    {
        if (!Resolve(target, ref targetTube))
            return null;

        var oppositeDirection = nextDirection.GetOpposite();
        var xform = Transform(target);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var currentTile = _mapSystem.CoordinatesToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var offset = nextDirection.ToIntVec();
        var targetTile = currentTile + offset;

        var anchoredEntities = _mapSystem.GetAnchoredEntities(xform.GridUid.Value, grid, targetTile);

        foreach (var entity in anchoredEntities)
        {
            if (!TryComp(entity, out VentCrawTubeComponent? tube))
                continue;

            if (ArePipesActuallyConnected(target, entity, nextDirection, oppositeDirection, pipeLayer))
                return entity;
        }

        return null;
    }

    public bool TryInsert(EntityUid uid, EntityUid entity, VentCrawEntryComponent? entry = null)
    {
        if (!Resolve(uid, ref entry))
            return false;

        if (!TryComp<VentCrawlerComponent>(entity, out var ventCrawlerComponent))
            return false;

        var xform = Transform(uid);
        var mapPos = _transformSystem.GetMapCoordinates(uid, xform: xform);
        var holder = Spawn(VentCrawEntryComponent.HolderPrototypeId, mapPos);
        var holderComponent = Comp<VentCrawHolderComponent>(holder);

        _ventCrawableSystem.TryInsert(holder, entity, holderComponent);

        _mover.SetRelay(entity, holder);
        ventCrawlerComponent.InTube = true;
        Dirty(entity, ventCrawlerComponent);

        return _ventCrawableSystem.EnterTube(holder, uid, holderComponent);
    }

    private void OnComponentInit(EntityUid uid, VentCrawTubeComponent tube, ComponentInit args)
    {
        tube.Contents = _containerSystem.EnsureContainer<Container>(uid, tube.ContainerId);
    }

    private void OnComponentRemove(EntityUid uid, VentCrawTubeComponent tube, ComponentRemove args)
    {
        DisconnectTube(uid, tube);
    }

    private void OnShutdown(EntityUid uid, VentCrawTubeComponent tube, ComponentShutdown args)
    {
        DisconnectTube(uid, tube);
    }

    private void OnStartup(EntityUid uid, VentCrawTubeComponent component, ComponentStartup args)
    {
        UpdateAnchored(uid, component, Transform(uid).Anchored);
    }

    private void OnDeconstruct(EntityUid uid, VentCrawTubeComponent component, ConstructionBeforeDeleteEvent args)
    {
        DisconnectTube(uid, component);
    }

    private void OnBreak(EntityUid uid, VentCrawTubeComponent component, BreakageEventArgs args)
    {
        DisconnectTube(uid, component);
    }

    private void OnAnchorChange(EntityUid uid, VentCrawTubeComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateAnchored(uid, component, args.Anchored);
    }

    private void OnGetBendConnectableDirections(EntityUid uid, VentCrawBendComponent component, ref GetVentCrawsConnectableDirectionsEvent args)
    {
        var direction = Transform(uid).LocalRotation;
        var side = new Angle(MathHelper.DegreesToRadians(direction.Degrees - 90));

        args.Connectable = new[] { direction.GetDir(), side.GetDir() };
    }

    private void OnGetEntryConnectableDirections(EntityUid uid, VentCrawEntryComponent component, ref GetVentCrawsConnectableDirectionsEvent args)
    {
        // Entry points should connect in all directions for easy exit
        args.Connectable = new[] {
            Direction.North, Direction.South, Direction.East, Direction.West,
            Direction.NorthEast, Direction.NorthWest, Direction.SouthEast, Direction.SouthWest
        };
    }

    private void OnGetJunctionConnectableDirections(EntityUid uid, VentCrawJunctionComponent component, ref GetVentCrawsConnectableDirectionsEvent args)
    {
        var direction = Transform(uid).LocalRotation;

        args.Connectable = component.Degrees
            .Select(degree => new Angle(degree.Theta + direction.Theta).GetDir())
            .ToArray();
    }

    private void OnGetTransitConnectableDirections(EntityUid uid, VentCrawTransitComponent component, ref GetVentCrawsConnectableDirectionsEvent args)
    {
        var rotation = Transform(uid).LocalRotation;
        var opposite = new Angle(rotation.Theta + Math.PI);

        args.Connectable = new[] { rotation.GetDir(), opposite.GetDir() };
    }

    private void AddClimbedVerb(EntityUid uid, VentCrawEntryComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<VentCrawlerComponent>(args.User, out var ventCrawlerComponent))
            return;

        if (TryComp(uid, out TransformComponent? transformComponent) && !transformComponent.Anchored)
            return;

        var isInside = ventCrawlerComponent.InTube && TryComp<BeingVentCrawComponent>(args.User, out var beingVentCraw)
            && TryComp<VentCrawHolderComponent>(beingVentCraw.Holder, out var holder) && holder.CurrentTube == uid;

        AlternativeVerb verb = new()
        {
            Act = isInside ?
                () => TryExit(uid, args.User) :
                () => TryEnter(uid, args.User),
            Text = isInside ?
                Loc.GetString("vent-craw-verb-exit") :
                Loc.GetString("vent-craw-verb-enter"),
            Icon = isInside ?
                new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")) :
                new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }

    private void OnGetManifoldVerbs(EntityUid uid, VentCrawManifoldComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryComp<VentCrawlerComponent>(args.User, out var ventCrawler) || !ventCrawler.InTube)
            return;

        if (!TryComp<BeingVentCrawComponent>(args.User, out var beingVentCraw) ||
            !TryComp<VentCrawHolderComponent>(beingVentCraw.Holder, out var holder) ||
            holder.CurrentTube != uid)
            return;

        if (!TryComp<VentCrawJunctionComponent>(uid, out var junction))
            return;

        var rotation = Transform(uid).LocalRotation;
        var availableDirs = junction.Degrees
            .Select(degree => new Angle(degree.Theta + rotation.Theta).GetDir())
            .ToList();

        for (int layer = 0; layer < 3; layer++)
        {
            if (layer == holder.PreviousPipeLayer)
                continue;

            foreach (var direction in availableDirs)
            {
                var nextTube = NextTubeFor(uid, direction, layer);
                if (nextTube == null)
                    continue;

                var layerName = layer switch
                {
                    0 => Loc.GetString("vent-craw-layer-primary"),
                    1 => Loc.GetString("vent-craw-layer-secondary"),
                    2 => Loc.GetString("vent-craw-layer-tertiary"),
                    _ => Loc.GetString("vent-craw-unknown")
                };

                var currentLayer = layer;
                var v = new Verb
                {
                    Priority = 1,
                    Category = VerbCategory.SelectType,
                    Text = $"{GetDirectionName(direction)} ({layerName})",
                    Impact = LogImpact.Low,
                    DoContactInteraction = true,
                    Act = () =>
                    {
                        TryMoveToDirection(uid, args.User, direction, holder, currentLayer);
                    }
                };
                args.Verbs.Add(v);
            }
        }
    }

    private void OnDoAfterEnterTube(EntityUid uid, VentCrawlerComponent component, EnterVentDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        TryInsert(args.Target.Value, args.User);
        args.Handled = true;
    }

    private void TryExit(EntityUid uid, EntityUid user)
    {
        if (!TryComp<BeingVentCrawComponent>(user, out var beingVentCraw) ||
            !TryComp<VentCrawHolderComponent>(beingVentCraw.Holder, out var holder))
            return;

        if (holder.CurrentTube != uid)
            return;

        if (TryComp<WeldableComponent>(uid, out var weldableComponent) && weldableComponent.IsWelded)
        {
            _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
            return;
        }

        _ventCrawableSystem.ExitVentCraws(beingVentCraw.Holder, holder);
    }

    private void TryEnter(EntityUid uid, EntityUid user, VentCrawlerComponent? crawler = null)
    {
        if (!Resolve(user, ref crawler))
            return;

        if (TryComp<WeldableComponent>(uid, out var weldableComponent))
        {
            if (weldableComponent.IsWelded)
            {
                _popup.PopupEntity(Loc.GetString("entity-storage-component-welded-shut-message"), user);
                return;
            }
        }

        var args = new DoAfterArgs(EntityManager, user, crawler.EnterDelay, new EnterVentDoAfterEvent(), user, uid, user)
        {
            BreakOnMove = true,
            BreakOnDamage = false
        };

        _doAfterSystem.TryStartDoAfter(args);
    }

    private void TryMoveToDirection(EntityUid manifoldUid, EntityUid user, Direction direction, VentCrawHolderComponent holder, int pipeLayer)
    {
        if (holder.CurrentTube != manifoldUid)
            return;

        if (!TryComp<BeingVentCrawComponent>(user, out var beingVentCraw))
            return;

        var nextTube = NextTubeFor(manifoldUid, direction, pipeLayer);
        if (nextTube == null)
            return;

        holder.PreviousPipeLayer = pipeLayer;
        if (_ventCrawableSystem.EnterTube(beingVentCraw.Holder, nextTube.Value, holder))
        {
            if (_gameTiming.CurTime > holder.LastCrawl + VentCrawableSystem.CrawlDelay)
            {
                holder.LastCrawl = _gameTiming.CurTime;
                _audioSystem.PlayPvs(holder.CrawlSound, user);
            }
        }
    }

    private void UpdateAnchored(EntityUid uid, VentCrawTubeComponent component, bool anchored)
    {
        if (anchored)
        {
            ConnectTube(uid, component);
        }
        else
        {
            DisconnectTube(uid, component);
        }
    }

    private bool ArePipesActuallyConnected(EntityUid pipeA, EntityUid pipeB, Direction directionFromA, Direction directionFromB, int pipeLayer = 0)
    {
        if (!TryComp<NodeContainerComponent>(pipeA, out var nodeContainerA) ||
            !TryComp<NodeContainerComponent>(pipeB, out var nodeContainerB))
            return false;

        var nodesA = nodeContainerA.Nodes.Values;
        var nodesB = nodeContainerB.Nodes.Values;

        foreach (var nodeA in nodesA)
        {
            if (nodeA is not PipeNode pipeNodeA)
                continue;

            if ((int)pipeNodeA.CurrentPipeLayer != pipeLayer)
                continue;

            if (!PipeDirectionHasDirection(pipeNodeA.CurrentPipeDirection, directionFromA))
                continue;

            foreach (var nodeB in nodesB)
            {
                if (nodeB is not PipeNode pipeNodeB)
                    continue;

                if ((int)pipeNodeB.CurrentPipeLayer != pipeLayer)
                    continue;

                if (!PipeDirectionHasDirection(pipeNodeB.CurrentPipeDirection, directionFromB))
                    continue;

                if (pipeNodeA.NodeGroup != null && pipeNodeB.NodeGroup != null &&
                    pipeNodeA.NodeGroup == pipeNodeB.NodeGroup)
                    return true;

                if (WouldPipesNormallyConnect(pipeNodeA, pipeNodeB, directionFromA, directionFromB))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a PipeDirection has a specific Direction
    /// </summary>
    private bool PipeDirectionHasDirection(PipeDirection pipeDirection, Direction direction)
    {
        var targetPipeDir = DirectionToPipeDirection(direction);
        return (pipeDirection & targetPipeDir) == targetPipeDir;
    }

    /// <summary>
    /// Fallback method to check if pipes would connect based on their directions and layers
    /// </summary>
    private bool WouldPipesNormallyConnect(PipeNode pipeA, PipeNode pipeB, Direction directionFromA, Direction directionFromB)
    {
        // Check if they have the same node group ID
        if (pipeA.NodeGroupID != pipeB.NodeGroupID)
            return false;

        // Check if they're on the same pipe layer
        if (pipeA.CurrentPipeLayer != pipeB.CurrentPipeLayer)
            return false;

        return PipeDirectionHasDirection(pipeA.CurrentPipeDirection, directionFromA)
            && PipeDirectionHasDirection(pipeB.CurrentPipeDirection, directionFromB);
    }

    /// <summary>
    /// Convert Direction to PipeDirection
    /// </summary>
    private PipeDirection DirectionToPipeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => PipeDirection.North,
            Direction.South => PipeDirection.South,
            Direction.East => PipeDirection.East,
            Direction.West => PipeDirection.West,
            Direction.NorthEast => PipeDirection.North | PipeDirection.East,
            Direction.NorthWest => PipeDirection.North | PipeDirection.West,
            Direction.SouthEast => PipeDirection.South | PipeDirection.East,
            Direction.SouthWest => PipeDirection.South | PipeDirection.West,
            _ => PipeDirection.None
        };
    }

    private static void ConnectTube(EntityUid _, VentCrawTubeComponent tube)
    {
        if (tube.Connected)
            return;

        tube.Connected = true;
    }

    private void DisconnectTube(EntityUid _, VentCrawTubeComponent tube)
    {
        if (!tube.Connected)
            return;

        tube.Connected = false;

        var query = GetEntityQuery<VentCrawHolderComponent>();
        foreach (var entity in tube.Contents.ContainedEntities.ToArray())
        {
            if (query.TryGetComponent(entity, out var holder))
                _ventCrawableSystem.ExitVentCraws(entity, holder);
        }
    }

    private string GetDirectionName(Direction direction)
    {
        return direction switch
        {
            Direction.North => Loc.GetString("vent-craw-direction-north"),
            Direction.South => Loc.GetString("vent-craw-direction-south"),
            Direction.East => Loc.GetString("vent-craw-direction-east"),
            Direction.West => Loc.GetString("vent-craw-direction-west"),
            Direction.NorthEast => Loc.GetString("vent-craw-direction-northeast"),
            Direction.NorthWest => Loc.GetString("vent-craw-direction-northwest"),
            Direction.SouthEast => Loc.GetString("vent-craw-direction-southeast"),
            Direction.SouthWest => Loc.GetString("vent-craw-direction-southwest"),
            _ => Loc.GetString("vent-craw-unknown")
        };
    }
}
