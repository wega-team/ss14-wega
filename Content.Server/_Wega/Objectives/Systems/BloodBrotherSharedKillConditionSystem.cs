using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

public sealed class BloodBrotherSharedKillConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly BloodBrotherSharedConditionSystem _sharedCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherSharedKillConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherSharedKillConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(uid, target.Value, comp.RequireDead, comp.RequireMaroon, args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid objectiveUid, EntityUid target, bool requireDead, bool requireMaroon, EntityUid mindId, MindComponent mind)
    {
        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mindId, out var sharedCondition)
            && !_sharedCondition.CheckBaseConditions(mindId, sharedCondition, mind))
            return 0f;

        return CalculateKillProgress(target, requireDead, requireMaroon);
    }

    private float CalculateKillProgress(EntityUid target, bool requireDead, bool requireMaroon)
    {
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        var targetDead = _mind.IsCharacterDeadIc(mind);
        var targetMarooned = !_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) || _mind.IsCharacterUnrevivableIc(mind);

        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled) && requireMaroon)
        {
            requireDead = true;
            requireMaroon = false;
        }

        if (requireDead && !targetDead)
            return 0f;

        if (requireMaroon && !_emergencyShuttle.EmergencyShuttleArrived)
            return 0f;

        if (requireMaroon && !_emergencyShuttle.ShuttlesLeft)
            return targetMarooned ? 0.5f : 0f;

        if (requireMaroon && _emergencyShuttle.ShuttlesLeft)
            return targetMarooned ? 1f : 0f;

        return 1f;
    }

    public void CopySharedKillConditionData(EntityUid sourceObjective, EntityUid targetObjective)
    {
        if (TryComp<BloodBrotherSharedKillConditionComponent>(sourceObjective, out var sourceCondition)
            && TryComp<BloodBrotherSharedKillConditionComponent>(targetObjective, out var targetCondition))
        {
            targetCondition.RequireDead = sourceCondition.RequireDead;
            targetCondition.RequireMaroon = sourceCondition.RequireMaroon;
        }
    }
}
