using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;

namespace Content.Server.Objectives.Systems;

public sealed class BloodBrotherSharedHijackConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly BloodBrotherSharedConditionSystem _sharedCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherSharedHijackConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherSharedHijackConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(uid, args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid objectiveUid, EntityUid mindId, MindComponent mind)
    {
        if (!CheckBaseHijackConditions(objectiveUid, mindId, mind))
            return 0f;

        if (!_emergencyShuttle.EmergencyShuttleArrived)
            return 0f;

        foreach (var stationData in EntityQuery<StationEmergencyShuttleComponent>())
        {
            if (stationData.EmergencyShuttle == null)
                continue;

            if (IsShuttleHijackedByBloodBrothers(stationData.EmergencyShuttle.Value, objectiveUid, mindId))
                return 1f;
        }

        return 0f;
    }

    private bool CheckBaseHijackConditions(EntityUid objectiveUid, EntityUid mindId, MindComponent mind)
    {
        if (mind.OwnedEntity == null || TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
            return false;

        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mindId, out var sharedCondition)
            && !_sharedCondition.CheckBaseConditions(mindId, sharedCondition, mind))
            return false;

        return true;
    }

    private bool IsShuttleHijackedByBloodBrothers(EntityUid shuttleGridId, EntityUid objectiveUid, EntityUid mindId)
    {
        var gridPlayers = Filter.BroadcastGrid(shuttleGridId).Recipients;
        var humanoids = GetEntityQuery<HumanoidAppearanceComponent>();
        var cuffable = GetEntityQuery<CuffableComponent>();
        EntityQuery<MobStateComponent>();

        var firstBrotherOnShuttle = false;
        var secondBrotherOnShuttle = false;
        EntityUid? brotherMindId = null;

        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mindId, out var sharedCondition)
            && sharedCondition.BrotherMind.HasValue)
        {
            brotherMindId = sharedCondition.BrotherMind.Value;
        }

        foreach (var player in gridPlayers)
        {
            if (player.AttachedEntity == null ||
                !_mind.TryGetMind(player.AttachedEntity.Value, out var crewMindId, out _))
                continue;

            if (mindId == crewMindId)
            {
                firstBrotherOnShuttle = true;
                continue;
            }

            if (brotherMindId.HasValue && brotherMindId.Value == crewMindId)
            {
                secondBrotherOnShuttle = true;
                continue;
            }

            var isHumanoid = humanoids.HasComponent(player.AttachedEntity.Value);
            if (!isHumanoid)
                continue;

            var isAntagonist = _role.MindIsAntagonist(crewMindId);
            if (isAntagonist)
                continue;

            var isPersonIncapacitated = _mobState.IsIncapacitated(player.AttachedEntity.Value);
            if (isPersonIncapacitated)
                continue;

            var isPersonCuffed =
                cuffable.TryGetComponent(player.AttachedEntity.Value, out var cuffed)
                && cuffed.CuffedHandCount > 0;
            if (isPersonCuffed)
                continue;

            return false;
        }

        return firstBrotherOnShuttle && secondBrotherOnShuttle;
    }
}
