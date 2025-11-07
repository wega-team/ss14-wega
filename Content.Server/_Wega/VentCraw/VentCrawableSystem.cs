using System.Linq;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Body.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.NodeContainer;
using Content.Shared.Tools.Components;
using Content.Shared.VentCraw;
using Content.Shared.VentCraw.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.VentCraw;

public sealed class VentCrawableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly VentCrawTubeSystem _ventCrawTubeSystem = default!;

    public static readonly TimeSpan CrawlDelay = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawHolderComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<VentCrawHolderComponent, MoveInputEvent>(OnMoveInput);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<VentCrawHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.CurrentDirection == Direction.Invalid)
                continue;

            var currentTube = holder.CurrentTube;
            if (currentTube == null)
                continue;

            if (holder.IsMoving && holder.NextTube == null)
            {
                if (HasComp<VentCrawManifoldComponent>(currentTube.Value))
                {
                    holder.NextTube = null;
                    holder.CurrentDirection = Direction.Invalid;
                    continue;
                }

                var nextTube = _ventCrawTubeSystem.NextTubeFor(currentTube.Value, holder.CurrentDirection, holder.PreviousPipeLayer);

                if (nextTube != null)
                {
                    if (!EntityManager.EntityExists(holder.CurrentTube))
                    {
                        ExitVentCraws(uid, holder);
                        continue;
                    }

                    holder.NextTube = nextTube;
                    holder.StartingTime = holder.Speed;
                    holder.TimeLeft = holder.Speed;
                }
                else
                {
                    var ev = new GetVentCrawsConnectableDirectionsEvent();
                    RaiseLocalEvent(currentTube.Value, ref ev);

                    if (ev.Connectable.Contains(holder.CurrentDirection))
                    {
                        if (HasComp<VentCrawEntryComponent>(currentTube.Value))
                        {
                            ExitVentCraws(uid, holder);
                            continue;
                        }

                        var oppositeDirection = holder.CurrentDirection.GetOpposite();
                        var canGoBack = _ventCrawTubeSystem.NextTubeFor(currentTube.Value, oppositeDirection) != null;

                        if (!canGoBack)
                        {
                            ExitVentCraws(uid, holder);
                            continue;
                        }
                    }

                    holder.NextTube = null;
                    holder.CurrentDirection = Direction.Invalid;
                }
            }

            if (holder.NextTube != null && holder.TimeLeft > 0)
            {
                var time = frameTime;
                if (time > holder.TimeLeft)
                    time = holder.TimeLeft;

                var progress = 1 - holder.TimeLeft / holder.StartingTime;
                var origin = Transform(currentTube.Value).Coordinates;
                var target = Transform(holder.NextTube.Value).Coordinates;
                var newPosition = (target.Position - origin.Position) * progress;

                var newCoords = origin.Offset(newPosition);
                _xformSystem.SetCoordinates(uid, _xformSystem.WithEntityId(newCoords, currentTube.Value));

                holder.TimeLeft -= time;
            }
            else if (holder.NextTube != null && holder.TimeLeft <= 0)
            {
                var tubeComp = Comp<VentCrawTubeComponent>(currentTube.Value);
                _containerSystem.Remove(uid, tubeComp.Contents, force: true);

                if (holder.FirstEntry)
                    holder.FirstEntry = false;

                if (HasComp<VentCrawEntryComponent>(holder.NextTube.Value))
                {
                    var welded = false;
                    if (TryComp<WeldableComponent>(holder.NextTube.Value, out var weldableComponent))
                        welded = weldableComponent.IsWelded;

                    if (!welded)
                    {
                        ExitVentCraws(uid, holder);
                        continue;
                    }
                    else
                    {
                        _containerSystem.Insert(uid, tubeComp.Contents);
                        holder.NextTube = null;
                        holder.CurrentDirection = Direction.Invalid;
                        continue;
                    }
                }

                if (_gameTiming.CurTime > holder.LastCrawl + CrawlDelay)
                {
                    holder.LastCrawl = _gameTiming.CurTime;
                    _audioSystem.PlayPvs(holder.CrawlSound, uid);
                }

                if (EntityManager.EntityExists(holder.NextTube.Value))
                {
                    var success = EnterTube(uid, holder.NextTube.Value, holder);
                    if (!success)
                    {
                        _containerSystem.Insert(uid, tubeComp.Contents);
                        holder.NextTube = null;
                        holder.CurrentDirection = Direction.Invalid;
                    }
                    else
                    {
                        holder.NextTube = null;
                    }
                }
                else
                {
                    ExitVentCraws(uid, holder);
                }
            }
        }
    }

    public bool EnterTube(EntityUid holderUid, EntityUid toUid, VentCrawHolderComponent? holder = null,
        VentCrawTubeComponent? to = null)
    {
        if (!Resolve(holderUid, ref holder))
            return false;

        if (holder.IsExitingVentCraws)
            return false;

        if (!Resolve(toUid, ref to))
        {
            ExitVentCraws(holderUid, holder);
            return false;
        }

        foreach (var ent in holder.Container.ContainedEntities)
        {
            var comp = EnsureComp<BeingVentCrawComponent>(ent);
            comp.Holder = holderUid;
        }

        if (!_containerSystem.Insert(holderUid, to.Contents))
        {
            ExitVentCraws(holderUid, holder);
            return false;
        }

        if (holder.CurrentTube != null)
        {
            holder.PreviousTube = holder.CurrentTube;
            holder.PreviousDirection = holder.CurrentDirection;
        }
        holder.CurrentTube = toUid;

        if (TryComp<NodeContainerComponent>(toUid, out var nodeContainer)
            && !HasComp<VentCrawManifoldComponent>(toUid))
        {
            var firstPipeNode = nodeContainer.Nodes.Values.OfType<PipeNode>().FirstOrDefault();
            if (firstPipeNode != null)
            {
                holder.PreviousPipeLayer = (int)firstPipeNode.CurrentPipeLayer;
            }
        }

        return true;
    }

    public void ExitVentCraws(EntityUid uid, VentCrawHolderComponent? holder = null)
    {
        if (!Exists(uid))
            return;

        if (!Resolve(uid, ref holder))
            return;

        if (holder.IsExitingVentCraws)
            return;

        holder.IsExitingVentCraws = true;

        foreach (var entity in holder.Container.ContainedEntities.ToArray())
        {
            RemComp<BeingVentCrawComponent>(entity);

            _containerSystem.Remove(entity, holder.Container, force: true);
            if (TryComp<VentCrawlerComponent>(entity, out var ventCrawlerComponent))
            {
                ventCrawlerComponent.InTube = false;
                Dirty(entity, ventCrawlerComponent);
            }

            var xform = Transform(entity);
            _xformSystem.AttachToGridOrMap(entity, xform);

            if (TryComp<PhysicsComponent>(entity, out var physics))
                _physicsSystem.WakeBody(entity, body: physics);
        }

        EntityManager.DeleteEntity(uid);
    }

    public bool TryInsert(EntityUid uid, EntityUid toInsert, VentCrawHolderComponent? holder = null)
    {
        if (!Resolve(uid, ref holder))
            return false;

        if (!CanInsert(uid, toInsert, holder))
            return false;

        if (!_containerSystem.Insert(toInsert, holder.Container))
            return false;

        if (TryComp<PhysicsComponent>(toInsert, out var physBody))
            _physicsSystem.SetCanCollide(toInsert, false, body: physBody);

        return true;
    }

    private bool CanInsert(EntityUid uid, EntityUid toInsert, VentCrawHolderComponent? holder = null)
    {
        if (!Resolve(uid, ref holder))
            return false;

        return HasComp<ItemComponent>(toInsert) || HasComp<BodyComponent>(toInsert);
    }

    private void OnComponentStartup(EntityUid uid, VentCrawHolderComponent holder, ComponentStartup args)
    {
        holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(VentCrawHolderComponent));
    }

    private void OnMoveInput(EntityUid uid, VentCrawHolderComponent component, ref MoveInputEvent args)
    {
        if (!EntityManager.EntityExists(component.CurrentTube))
        {
            ExitVentCraws(uid, component);
            return;
        }

        component.IsMoving = (args.OldMovement & MoveButtons.AnyDirection) != MoveButtons.None;

        if (component.IsMoving)
        {
            var newDirection = GetDirectionFromMoveButtons(args.OldMovement);

            if (HasComp<VentCrawManifoldComponent>(component.CurrentTube)
                && component.CurrentDirection == Direction.Invalid)
            {
                var nextTube = _ventCrawTubeSystem.NextTubeFor(component.CurrentTube.Value, newDirection, component.PreviousPipeLayer);
                if (nextTube != null)
                {
                    component.CurrentDirection = newDirection;
                    component.NextTube = nextTube;
                    component.StartingTime = component.Speed;
                    component.TimeLeft = component.Speed;
                }
            }
            else
            {
                component.CurrentDirection = newDirection;
            }
        }
        else
        {
            component.CurrentDirection = Direction.Invalid;
        }
    }

    private Direction GetDirectionFromMoveButtons(MoveButtons buttons)
    {
        var hasUp = (buttons & MoveButtons.Up) != MoveButtons.None;
        var hasDown = (buttons & MoveButtons.Down) != MoveButtons.None;
        var hasLeft = (buttons & MoveButtons.Left) != MoveButtons.None;
        var hasRight = (buttons & MoveButtons.Right) != MoveButtons.None;

        if (hasUp && hasRight) return Direction.NorthEast;
        if (hasUp && hasLeft) return Direction.NorthWest;
        if (hasDown && hasRight) return Direction.SouthEast;
        if (hasDown && hasLeft) return Direction.SouthWest;

        if (hasUp) return Direction.North;
        if (hasDown) return Direction.South;
        if (hasLeft) return Direction.West;
        if (hasRight) return Direction.East;

        return Direction.Invalid;
    }
}
