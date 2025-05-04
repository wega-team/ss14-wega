using System.Threading;
using System.Threading.Tasks;
using Content.Server.Xenobiology;
using Content.Shared.Xenobiology.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SlimeEatOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [DataField("targetKey")]
    public string TargetKey = "FoodTarget";

    [DataField("eatRange")]
    public float EatRange = 1.5f;

    [DataField("cooldown")]
    public float Cooldown = 6f;

    private TimeSpan _lastAttemptTime;
    private bool _isEating;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var food, _entMan) ||
            !_entMan.EntityExists(food) ||
            _entMan.Deleted(food))
        {
            return (false, null);
        }

        var transformSys = _entMan.System<SharedTransformSystem>();
        var distance = (transformSys.GetWorldPosition(owner) -
                       transformSys.GetWorldPosition(food)).Length();

        if (distance > EatRange)
            return (false, null);

        return (true, null);
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        _isEating = false;
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < _lastAttemptTime + TimeSpan.FromSeconds(Cooldown))
            return HTNOperatorStatus.Continuing;

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var food, _entMan) ||
            !_entMan.EntityExists(food) ||
            _entMan.Deleted(food))
        {
            return HTNOperatorStatus.Failed;
        }

        if (!_isEating)
        {
            var hunger = _entMan.GetComponent<SlimeHungerComponent>(owner);
            var hungerSystem = _entMan.System<SlimeHungerSystem>();
            _isEating = hungerSystem.TryFeedSlime(owner, food, hunger);
            _lastAttemptTime = currentTime;

            if (!_isEating)
                return HTNOperatorStatus.Failed;
        }

        if (currentTime >= _lastAttemptTime + TimeSpan.FromSeconds(Cooldown))
        {
            _isEating = false;
            return HTNOperatorStatus.Finished;
        }

        return HTNOperatorStatus.Continuing;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        _isEating = false;

        if (status == HTNOperatorStatus.Finished)
        {
            blackboard.Remove<EntityCoordinates>("TargetCoordinates");
        }
    }
}
