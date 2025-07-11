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

        var target = _entMan.EntityQuery<HumanoidAppearanceComponent>()
            .Select(x => x.Owner)
            .Where(x =>
            {
                if (!_entMan.TryGetComponent<TransformComponent>(x, out var xform))
                    return false;

                if (_entMan.TryGetComponent<SlimeSocialComponent>(owner, out var social))
                    if (social.Friends.Contains(x))
                        return false;

                if (_entMan.TryGetComponent<MobStateComponent>(x, out var mobState) &&
                    mobState.CurrentState == MobState.Dead)
                    return false;

                return xform.Coordinates.TryDistance(_entMan, ownerTransform.Coordinates, out var dist) &&
                        dist <= range;
            })
            .OrderBy(x =>
            {
                var xform = _entMan.GetComponent<TransformComponent>(x);
                return xform.Coordinates.TryDistance(_entMan, ownerTransform.Coordinates, out var dist)
                    ? dist
                    : float.MaxValue;
            })
            .FirstOrDefault();

        if (target == default)
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetKey, target },
            { "TargetCoordinates", _entMan.GetComponent<TransformComponent>(target).Coordinates }
        });
    }
}
