using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class BloodBrotherSharedEscapeConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly BloodBrotherSharedConditionSystem _sharedCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherSharedEscapeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherSharedEscapeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid objectiveUid, EntityUid mindId, MindComponent mind)
    {
        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mindId, out var sharedCondition)
            && !_sharedCondition.CheckBaseConditions(mindId, sharedCondition, mind))
            return 0f;

        var currentEscape = CheckEscape(mindId, mind);

        var brotherEscape = 0f;
        if (sharedCondition?.BrotherMind != null &&
            TryComp<MindComponent>(sharedCondition.BrotherMind.Value, out var brotherMind))
        {
            brotherEscape = CheckEscape(sharedCondition.BrotherMind.Value, brotherMind);
        }

        return Math.Min(currentEscape, brotherEscape);
    }

    private float CheckEscape(EntityUid mindId, MindComponent mind)
    {
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
            return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) ? 0.5f : 0f;

        return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) ? 1f : 0f;
    }
}
