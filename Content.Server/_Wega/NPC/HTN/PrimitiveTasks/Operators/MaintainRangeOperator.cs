using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;
using Content.Server.NPC.Components;
using Content.Server.Lavaland.Mobs;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class MegafaunaMaintainRangeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;
    private SharedTransformSystem _transformSystem = default!;
    private MegafaunaSystem _megafauna = default!;

    [DataField("minRange")]
    public float MinRange = 3f;

    [DataField("maxRange")]
    public float MaxRange = 5f;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
        _transformSystem = sysManager.GetEntitySystem<SharedTransformSystem>();
        _megafauna = sysManager.GetEntitySystem<MegafaunaSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var target = _megafauna.FindAttackTarget(owner);
        if (target == null)
            return (false, null);

        var ownerXform = _entMan.GetComponent<TransformComponent>(owner);
        var targetXform = _entMan.GetComponent<TransformComponent>(target.Value);

        var ownerPos = _transformSystem.GetWorldPosition(ownerXform);
        var targetPos = _transformSystem.GetWorldPosition(targetXform);
        var currentDistance = (targetPos - ownerPos).Length();

        if (currentDistance >= MinRange && currentDistance <= MaxRange)
        {
            return (true, new Dictionary<string, object>
            {
                { "TargetCoordinates", ownerXform.Coordinates }
            });
        }

        if (currentDistance < MinRange)
        {
            var direction = (ownerPos - targetPos).Normalized();

            var idealDistance = (MinRange + MaxRange) / 2f;
            var awayPosition = targetPos + direction * idealDistance;

            var gridUid = targetXform.GridUid ?? targetXform.MapUid;
            if (gridUid == null)
                return (false, null);

            var awayCoords = new EntityCoordinates(gridUid.Value, awayPosition);

            var awayPath = await _pathfind.GetPath(
                owner,
                ownerXform.Coordinates,
                awayCoords,
                idealDistance * 2f,
                cancelToken,
                PathFlags.None);

            if (awayPath.Result == PathResult.Path)
            {
                return (true, new Dictionary<string, object>
                {
                    { "TargetCoordinates", awayPath.Path[^1].Coordinates }
                });
            }

            for (int i = 0; i < 5; i++)
            {
                var randomPath = await _pathfind.GetRandomPath(owner, MaxRange, cancelToken);
                if (randomPath.Result == PathResult.Path)
                {
                    var randomCoords = randomPath.Path[^1].Coordinates;
                    var randomPos = _transformSystem.ToMapCoordinates(randomCoords).Position;
                    var newDistance = (targetPos - randomPos).Length();

                    if (newDistance > currentDistance + 1f)
                    {
                        return (true, new Dictionary<string, object>
                        {
                            { "TargetCoordinates", randomCoords }
                        });
                    }
                }
            }
            return (false, null);
        }
        else
        {
            var approachPath = await _pathfind.GetPath(
                owner,
                ownerXform.Coordinates,
                targetXform.Coordinates,
                MaxRange,
                cancelToken,
                PathFlags.None);

            if (approachPath.Result == PathResult.Path)
            {
                return (true, new Dictionary<string, object>
                {
                    { "TargetCoordinates", approachPath.Path[^1].Coordinates }
                });
            }

            for (int i = 0; i < 5; i++)
            {
                var randomPath = await _pathfind.GetRandomPath(owner, MaxRange, cancelToken);
                if (randomPath.Result == PathResult.Path)
                {
                    var randomCoords = randomPath.Path[^1].Coordinates;
                    var randomPos = _transformSystem.ToMapCoordinates(randomCoords).Position;
                    var newDistance = (targetPos - randomPos).Length();

                    if (newDistance < currentDistance - 1f && newDistance >= MinRange)
                    {
                        return (true, new Dictionary<string, object>
                        {
                            { "TargetCoordinates", randomCoords }
                        });
                    }
                }
            }

            return (false, null);
        }
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        if (!blackboard.TryGetValue<EntityCoordinates>("TargetCoordinates", out var targetCoordinates, _entMan))
            return;

        var uid = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entMan.TryGetComponent<NPCSteeringComponent>(uid, out var steering))
        {
            _steering.Register(uid, targetCoordinates);
        }
        else
        {
            _steering.Register(uid, targetCoordinates, steering);
        }
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        var target = _megafauna.FindAttackTarget(owner);
        if (target == null)
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        var ownerXform = _entMan.GetComponent<TransformComponent>(owner);
        var targetXform = _entMan.GetComponent<TransformComponent>(target.Value);

        var ownerPos = _transformSystem.GetWorldPosition(ownerXform);
        var targetPos = _transformSystem.GetWorldPosition(targetXform);
        var currentDistance = (targetPos - ownerPos).Length();

        if (currentDistance >= MinRange && currentDistance <= MaxRange)
        {
            return HTNOperatorStatus.Finished;
        }

        if (steering.Status == SteeringStatus.InRange &&
            (currentDistance < MinRange || currentDistance > MaxRange))
        {
            return HTNOperatorStatus.Failed;
        }

        return steering.Status switch
        {
            SteeringStatus.InRange => HTNOperatorStatus.Finished,
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

        blackboard.Remove<EntityCoordinates>("TargetCoordinates");

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (_entMan.EntityExists(owner))
        {
            _steering.Unregister(owner);
        }
    }
}
