using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class BloodBrotherSharedKeepAliveConditionSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;
    [Dependency] private BloodBrotherSharedConditionSystem _sharedCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherSharedKeepAliveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherSharedKeepAliveConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid objectiveUid, EntityUid mindId, MindComponent mind)
    {
        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mindId, out var sharedCondition)
            && !_sharedCondition.CheckBaseConditions(mindId, sharedCondition, mind))
            return 0f;

        if (!_target.GetTarget(objectiveUid, out var target))
            return 0f;

        return CalculateProtectProgress(target.Value);
    }

    private float CalculateProtectProgress(EntityUid target)
    {
        if (!TryComp<MindComponent>(target, out var mind))
            return 0f;

        return _mind.IsCharacterDeadIc(mind) ? 0f : 1f;
    }
}
