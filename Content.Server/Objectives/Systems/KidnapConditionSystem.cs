using System.Linq;
using Content.Server.Objectives.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

public sealed class KidnapConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KidnapConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<KidnapConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<KidnapConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(EntityUid uid, KidnapConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (args.Cancelled || comp.AllowedJobs.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        // First gather all online players with any allowed job, grouped by job.
        // This ensures we only pick jobs that actually have players online.
        var byJob = new Dictionary<ProtoId<JobPrototype>, List<EntityUid>>();
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (!_job.MindTryGetJobId(mindId, out var jobId) || jobId == null)
                continue;
            if (!comp.AllowedJobs.Contains(jobId.Value))
                continue;
            if (mind.OwnedEntity is not { } body || !HasComp<ActorComponent>(body))
                continue;

            if (!byJob.TryGetValue(jobId.Value, out var list))
            {
                list = new List<EntityUid>();
                byJob[jobId.Value] = list;
            }
            list.Add(mindId);
        }

        if (byJob.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        // Pick up to JobCount occupied jobs at random.
        var occupiedJobs = byJob.Keys.ToList();
        _random.Shuffle(occupiedJobs);
        comp.TargetJobs = occupiedJobs.Take(Math.Min(comp.JobCount, occupiedJobs.Count)).ToList();

        var candidates = comp.TargetJobs
            .SelectMany(j => byJob.TryGetValue(j, out var l) ? l : Enumerable.Empty<EntityUid>())
            .ToList();

        comp.Target = _random.Pick(candidates);
        comp.RequiredScans = _random.Next(comp.MinScans, comp.MaxScans + 1);
    }

    private void OnAfterAssign(EntityUid uid, KidnapConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        var jobNames = comp.TargetJobs
            .Select(j => _prototype.TryIndex(j, out var proto) ? proto.LocalizedName : j.Id)
            .ToList();

        _metaData.SetEntityName(uid,
            Loc.GetString("objective-condition-kidnap-title", ("jobs", string.Join(", ", jobNames))),
            args.Meta);
    }

    private void OnGetProgress(EntityUid uid, KidnapConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (comp.TargetScanned)
        {
            args.Progress = 1f;
            return;
        }

        if (comp.RequiredScans == 0)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = MathF.Min(comp.ScannedMinds.Count / (float) comp.RequiredScans, 1f);
    }

    /// <summary>
    /// Validates and records a mind-scan for the ninja's kidnap objective.
    /// Returns true if the scan was recorded or the objective is already complete.
    /// </summary>
    public bool TryRecordScan(EntityUid ninja, EntityUid occupant, out string? message)
    {
        message = null;

        if (!_mind.TryGetObjectiveComp<KidnapConditionComponent>(ninja, out var condition))
        {
            message = Loc.GetString("mind-scan-machine-no-objective");
            return false;
        }

        if (condition.TargetScanned || condition.ScannedMinds.Count >= condition.RequiredScans)
        {
            message = Loc.GetString("mind-scan-machine-already-complete");
            return true;
        }

        if (!_mind.TryGetMind(occupant, out var occupantMindId, out _))
        {
            message = Loc.GetString("mind-scan-machine-no-mind");
            return false;
        }

        if (_mobState.IsDead(occupant))
        {
            message = Loc.GetString("mind-scan-machine-dead");
            return false;
        }

        if (!HasComp<ActorComponent>(occupant))
        {
            message = Loc.GetString("mind-scan-machine-no-player");
            return false;
        }

        if (!HasComp<HumanoidProfileComponent>(occupant))
        {
            message = Loc.GetString("mind-scan-machine-not-humanoid");
            return false;
        }

        if (!_job.MindTryGetJobId(occupantMindId, out var jobId) || jobId == null || !condition.TargetJobs.Contains(jobId.Value))
        {
            message = Loc.GetString("mind-scan-machine-wrong-job");
            return false;
        }

        if (condition.ScannedMinds.Contains(occupantMindId))
        {
            message = Loc.GetString("mind-scan-machine-already-scanned");
            return false;
        }

        condition.ScannedMinds.Add(occupantMindId);

        var ninjaName = MetaData(ninja).EntityName;

        if (condition.Target == occupantMindId)
        {
            condition.TargetScanned = true;
            message = Loc.GetString("mind-scan-machine-target-found", ("ninjaName", ninjaName));
        }
        else if (condition.ScannedMinds.Count >= condition.RequiredScans)
        {
            message = Loc.GetString("mind-scan-machine-enough-scans", ("ninjaName", ninjaName));
        }
        else
        {
            message = Loc.GetString("mind-scan-machine-scan-recorded");
        }

        return true;
    }
}
