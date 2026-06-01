using Content.Server.Objectives.Components;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the three new randomised ninja objectives: Frame, Protect, and Intimidate.
/// </summary>
public sealed class NinjaNewObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaFrameConditionComponent, ObjectiveGetProgressEvent>(OnFrameGetProgress);

        SubscribeLocalEvent<PickKillTargetPersonComponent, ObjectiveAssignedEvent>(OnKillTargetAssigned);

        SubscribeLocalEvent<NinjaIntimidateConditionComponent, ObjectiveAfterAssignEvent>(OnIntimidateAfterAssign,
            after: new[] { typeof(TargetObjectiveSystem) });
        SubscribeLocalEvent<NinjaIntimidateTrackerComponent, DamageChangedEvent>(OnTrackerDamageChanged);
        SubscribeLocalEvent<NinjaIntimidateConditionComponent, ObjectiveGetProgressEvent>(OnIntimidateGetProgress);
        SubscribeLocalEvent<NinjaIntimidateConditionComponent, ComponentShutdown>(OnIntimidateShutdown);
    }

    // --- Frame ---

    private void OnFrameGetProgress(EntityUid uid, NinjaFrameConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var targetMind))
            return;

        if (!TryComp<MindComponent>(targetMind, out var mind) || mind.CharacterName == null)
            return;

        var targetName = mind.CharacterName;
        var query = EntityQueryEnumerator<StationRecordsComponent>();
        while (query.MoveNext(out var station, out var records))
        {
            var recordId = _stationRecords.GetRecordByName(station, targetName, records);
            if (recordId == null)
                continue;

            var key = new StationRecordKey(recordId.Value, station);
            if (!_stationRecords.TryGetRecord<CriminalRecord>(key, out var record, records))
                continue;

            if (record.Status != SecurityStatus.None)
            {
                args.Progress = 1f;
                return;
            }
        }

        args.Progress = 0f;
    }

    // --- Protect ---

    private void OnKillTargetAssigned(Entity<PickKillTargetPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var targetComp))
        {
            args.Cancelled = true;
            return;
        }

        if (targetComp.Target != null)
            return;

        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<KillPersonConditionComponent, TargetObjectiveComponent>();
        while (query.MoveNext(out _, out _, out var killTarget))
        {
            if (killTarget.Target is { } t)
                candidates.Add(t);
        }

        if (candidates.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent, _random.Pick(candidates), targetComp);
    }

    // --- Intimidate ---

    private void OnIntimidateAfterAssign(EntityUid uid, NinjaIntimidateConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        comp.DamageThreshold = _random.NextFloat(comp.MinDamage, comp.MaxDamage);

        if (!_target.GetTarget(uid, out var targetMind))
            return;

        if (!TryComp<MindComponent>(targetMind, out var mind) || mind.OwnedEntity == null)
            return;

        comp.TrackedEntity = mind.OwnedEntity;
        var tracker = EnsureComp<NinjaIntimidateTrackerComponent>(mind.OwnedEntity.Value);
        tracker.ConditionEntity = uid;

        var targetName = mind.CharacterName ?? "Unknown";
        var jobName = _job.MindTryGetJobName(targetMind.Value);
        _metaData.SetEntityName(uid,
            Loc.GetString("objective-condition-ninja-intimidate-title",
                ("targetName", targetName),
                ("job", jobName),
                ("threshold", (int) comp.DamageThreshold)),
            args.Meta);
    }

    private void OnTrackerDamageChanged(EntityUid uid, NinjaIntimidateTrackerComponent tracker, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!TryComp<NinjaIntimidateConditionComponent>(tracker.ConditionEntity, out var cond))
            return;

        var total = 0f;
        foreach (var (_, delta) in args.DamageDelta.DamageDict)
        {
            if (delta > 0)
                total += (float) delta;
        }

        cond.AccumulatedDamage += total;
    }

    private void OnIntimidateGetProgress(EntityUid uid, NinjaIntimidateConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var targetMind))
            return;

        if (!TryComp<MindComponent>(targetMind, out var mind))
            return;

        if (_mind.IsCharacterDeadIc(mind))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = MathF.Min(comp.AccumulatedDamage / comp.DamageThreshold, 1f);
    }

    private void OnIntimidateShutdown(EntityUid uid, NinjaIntimidateConditionComponent comp, ComponentShutdown args)
    {
        if (comp.TrackedEntity is { } tracked && !TerminatingOrDeleted(tracked))
            RemComp<NinjaIntimidateTrackerComponent>(tracked);
    }
}
