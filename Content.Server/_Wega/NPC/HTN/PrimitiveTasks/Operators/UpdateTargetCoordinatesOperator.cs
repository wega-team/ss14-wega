using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class UpdateTargetCoordinatesOperator : HTNOperator
{
    [DataField("targetKey")]
    public string TargetKey = default!;

    [DataField("outputKey")]
    public string OutputKey = default!;

    [Dependency] private readonly IEntityManager _entManager = default!;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);

        if (blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager))
        {
            var xform = _entManager.GetComponent<TransformComponent>(target);
            blackboard.SetValue(OutputKey, xform.Coordinates);
        }
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return HTNOperatorStatus.Finished;
    }

}
