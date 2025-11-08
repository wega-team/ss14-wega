using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Xenobiology;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SlimeFindTargetOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField("targetKey")]
    public string TargetKey = "AttackTarget";

    [DataField("rangeKey")]
    public string RangeKey = "AggroRange";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entMan))
            range = 5f;

        if (!_entMan.TryGetComponent<TransformComponent>(owner, out var ownerTransform))
            return (false, null);

        EntityUid target = default;
        float minDistance = float.MaxValue;

        var query = _entMan.EntityQueryEnumerator<HumanoidAppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (_entMan.TryGetComponent<SlimeSocialComponent>(owner, out var social) &&
                social.Friends.Contains(uid))
                continue;

            if (_entMan.TryGetComponent<MobStateComponent>(uid, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
                continue;

            if (!xform.Coordinates.TryDistance(_entMan, ownerTransform.Coordinates, out var dist) ||
                dist > range)
                continue;

            if (dist < minDistance)
            {
                minDistance = dist;
                target = uid;
            }
        }

        if (target == default)
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetKey, target },
            { "TargetCoordinates", _entMan.GetComponent<TransformComponent>(target).Coordinates }
        });
    }
}
