using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;
using Content.Server.NPC.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

public sealed partial class MaintainRangeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;
    private SharedTransformSystem _transform = default!;

    [DataField("targetKey", required: true)]
    public string TargetKey = default!;

    [DataField("minRange")]
    public float MinRange = 3f;

    [DataField("maxRange")]
    public float MaxRange = 5f;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    [DataField("targetCoordinatesKey")]
    public string TargetCoordinatesKey = "TargetCoordinates";

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) ||
            !_entMan.EntityExists(target))
        {
            return (false, null);
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entMan.TryGetComponent<TransformComponent>(owner, out var ownerXform) ||
            !_entMan.TryGetComponent<PhysicsComponent>(owner, out var body))
            return (false, null);

        var targetXform = _entMan.GetComponent<TransformComponent>(target);

        var ownerPos = _transform.GetWorldPosition(ownerXform);
        var targetPos = _transform.GetWorldPosition(targetXform);
        var currentDistance = (targetPos - ownerPos).Length();

        var direction = (ownerPos - targetPos).Normalized();
        var optimalDistance = (MinRange + MaxRange) / 2f;

        if (currentDistance >= MinRange && currentDistance <= MaxRange)
        {
            var relative = new EntityCoordinates(target, direction * optimalDistance);
            return (true, new Dictionary<string, object>
            {
                { TargetCoordinatesKey, relative }
            });
        }

        var desiredCoords = new EntityCoordinates(target, direction * optimalDistance);

        var path = await _pathfind.GetPath(
            owner,
            ownerXform.Coordinates,
            desiredCoords,
            optimalDistance,
            cancelToken,
            _pathfind.GetFlags(blackboard));

        if (path.Result != PathResult.Path)
        {
            return (false, null);
        }

        return (true, new Dictionary<string, object>
        {
            { TargetCoordinatesKey, desiredCoords },
            { PathfindKey, path }
        });
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        if (!blackboard.TryGetValue<EntityCoordinates>(TargetCoordinatesKey, out var targetCoords, _entMan))
            return;

        blackboard.Remove<EntityCoordinates>(TargetCoordinatesKey);

        var targetCoordinates = targetCoords;
        var uid = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var comp = _steering.Register(uid, targetCoordinates);

        var tolerance = (MaxRange - MinRange) / 2f;
        comp.Range = MathF.Max(comp.Range, tolerance);

        if (blackboard.TryGetValue<PathResultEvent>(PathfindKey, out var result, _entMan))
        {
            if (blackboard.TryGetValue<EntityCoordinates>(NPCBlackboard.OwnerCoordinates, out var coordinates, _entMan))
            {
                var mapCoords = _transform.ToMapCoordinates(coordinates);
                _steering.PrunePath(uid, mapCoords,
                    _transform.ToMapCoordinates(targetCoordinates).Position - mapCoords.Position,
                    result.Path);
            }

            comp.CurrentPath = new Queue<PathPoly>(result.Path);
        }
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) ||
            !_entMan.EntityExists(target))
        {
            return HTNOperatorStatus.Failed;
        }

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entMan.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        var ownerXform = _entMan.GetComponent<TransformComponent>(owner);
        var targetXform = _entMan.GetComponent<TransformComponent>(target);

        var ownerPos = _transform.GetWorldPosition(ownerXform);
        var targetPos = _transform.GetWorldPosition(targetXform);
        var currentDistance = (targetPos - ownerPos).Length();

        if (currentDistance >= MinRange && currentDistance <= MaxRange)
            return HTNOperatorStatus.Finished;

        return steering.Status switch
        {
            SteeringStatus.InRange => (currentDistance >= MinRange && currentDistance <= MaxRange) ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing,
            SteeringStatus.NoPath => HTNOperatorStatus.Failed,
            SteeringStatus.Moving => HTNOperatorStatus.Continuing,
            _ => HTNOperatorStatus.Failed
        };
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        if (blackboard.TryGetValue<CancellationTokenSource>(MovementCancelToken, out var cancelToken, _entMan))
        {
            cancelToken.Cancel();
            blackboard.Remove<CancellationTokenSource>(MovementCancelToken);
        }

        blackboard.Remove<PathResultEvent>(PathfindKey);
        blackboard.Remove<EntityCoordinates>(TargetCoordinatesKey);

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (_entMan.EntityExists(owner))
            _steering.Unregister(owner);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        if (status != HTNOperatorStatus.BetterPlan)
            ConditionalShutdown(blackboard);
    }

    public override void PlanShutdown(NPCBlackboard blackboard)
    {
        base.PlanShutdown(blackboard);
        ConditionalShutdown(blackboard);
    }
}
