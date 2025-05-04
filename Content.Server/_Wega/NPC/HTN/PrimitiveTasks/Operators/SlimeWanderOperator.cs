using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Systems;
using Robust.Shared.Map;
using Content.Server.NPC.Components;
using System.Linq;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SlimeWanderOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private NPCSteeringSystem _steering = default!;
    private PathfindingSystem _pathfind = default!;

    [DataField("rangeKey")]
    public string RangeKey = "WanderRange";

    [DataField("wanderRange")]
    public float DefaultWanderRange = 5f;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    private const string MovementCancelToken = "MovementCancelToken";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfind = sysManager.GetEntitySystem<PathfindingSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var range = blackboard.TryGetValue<float>(RangeKey, out var rangeVal, _entMan)
            ? rangeVal
            : DefaultWanderRange;

        var path = await _pathfind.GetRandomPath(owner, range, cancelToken);

        if (path.Result != PathResult.Path)
            return (false, null);

        var lastNode = path.Path.Last();
        return (true, new Dictionary<string, object>
        {
            { "TargetCoordinates", lastNode.Coordinates }
        });
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
        if (!_entMan.TryGetComponent<NPCSteeringComponent>(owner, out var steering))
            return HTNOperatorStatus.Failed;

        if (ShutdownState == HTNPlanState.PlanFinished && steering.Status == SteeringStatus.Moving)
            return HTNOperatorStatus.Finished;

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
        _steering.Unregister(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }
}
