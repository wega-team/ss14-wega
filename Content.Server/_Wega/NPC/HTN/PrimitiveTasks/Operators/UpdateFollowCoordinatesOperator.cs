using Content.Server.NPC.HTN.PrimitiveTasks;
using JetBrains.Annotations;

namespace Content.Server.NPC.HTN.Preconditions;

[UsedImplicitly]
public sealed partial class UpdateFollowCoordinatesOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("followTargetKey")]
    public string FollowTargetKey = "FollowTarget";

    [DataField("targetCoordinatesKey")]
    public string TargetCoordinatesKey = "TargetCoordinates";

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(FollowTargetKey, out var followTarget, _entManager) ||
            !_entManager.TryGetComponent<TransformComponent>(followTarget, out var targetXform))
        {
            return HTNOperatorStatus.Failed;
        }

        blackboard.SetValue(TargetCoordinatesKey, targetXform.Coordinates);
        return HTNOperatorStatus.Finished;
    }
}
