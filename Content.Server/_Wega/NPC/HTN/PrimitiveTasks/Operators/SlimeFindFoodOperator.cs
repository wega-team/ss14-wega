using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Xenobiology.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SlimeFindFoodOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField("targetKey")]
    public string TargetKey = "FoodTarget";

    [DataField("rangeKey")]
    public string RangeKey = "FoodSearchRange";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entMan))
            range = 5f;

        if (!_entMan.TryGetComponent<TransformComponent>(owner, out var ownerTransform))
            return (false, null);

        EntityUid food = default;
        float minDistance = float.MaxValue;

        var query = _entMan.EntityQueryEnumerator<SlimeFoodComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!xform.Coordinates.TryDistance(_entMan, ownerTransform.Coordinates, out var dist) ||
                dist > range)
                continue;

            if (dist < minDistance)
            {
                minDistance = dist;
                food = uid;
            }
        }

        if (food == default)
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetKey, food },
            { "TargetCoordinates", _entMan.GetComponent<TransformComponent>(food).Coordinates },
            { "MovementRange", 1.0f }
        });
    }
}
