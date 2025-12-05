using System.Text;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Blood.Brother;
using Content.Server.Roles;
using Content.Server.Objectives.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class BloodBrotherRuleSystem : GameRuleSystem<BloodBrotherRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly BloodBrotherSharedConditionSystem _sharedCondition = default!;
    [Dependency] private readonly BloodBrotherSharedStealConditionSystem _stealCondition = default!;
    [Dependency] private readonly BloodBrotherSharedKillConditionSystem _killCondition = default!;


    private static readonly Color BloodBrotherColor = Color.FromHex("#8b0000");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
    }

    private void AfterEntitySelected(Entity<BloodBrotherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        CreateBloodBrotherPair(args.EntityUid, ent);
    }

    /// <summary>
    /// Creates a pair of blood brothers
    /// </summary>
    public void CreateBloodBrotherPair(EntityUid bloodBrother, Entity<BloodBrotherRuleComponent> component)
    {
        if (!_mindSystem.TryGetMind(bloodBrother, out var mindId, out var mind))
            return;

        component.Comp.BloodBrotherMinds.Add(mindId);

        EntityUid? brotherMindId = FindUnpairedBrother(mindId, component.Comp);

        if (brotherMindId != null)
        {
            CreateBloodBrotherPairInternal(mindId, brotherMindId.Value, component);
            GenerateSharedObjectives(mindId, brotherMindId.Value, component);
        }
    }

    private void GenerateSharedObjectives(EntityUid mindId1, EntityUid mindId2, Entity<BloodBrotherRuleComponent> component)
    {
        if (!TryComp<MindComponent>(mindId1, out var mind1) || !TryComp<MindComponent>(mindId2, out var mind2))
            return;

        var currentDifficulty = 0f;
        var selectedObjectives = new List<EntityUid>();

        foreach (var set in component.Comp.ObjectiveSets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && component.Comp.MaxDifficulty > currentDifficulty; pick++)
            {
                var objective = _objectives.GetRandomObjective(mindId1, mind1, set.Groups, component.Comp.MaxDifficulty - currentDifficulty);
                if (objective == null)
                    continue;

                var objectiveComp = Comp<ObjectiveComponent>(objective.Value);
                currentDifficulty += objectiveComp.Difficulty;
                selectedObjectives.Add(objective.Value);
                Log.Debug($"Selected random objective {ToPrettyString(objective)} for blood brothers pair");
            }
        }

        foreach (var mandatoryObjectiveProto in component.Comp.RequiredObjectives)
        {
            var objective = _objectives.TryCreateObjective(mindId1, mind1, mandatoryObjectiveProto);
            if (objective != null)
            {
                var objectiveComp = Comp<ObjectiveComponent>(objective.Value);
                currentDifficulty += objectiveComp.Difficulty;
                selectedObjectives.Add(objective.Value);
                Log.Debug($"Added mandatory objective {mandatoryObjectiveProto} for blood brothers pair");
            }
            else
            {
                Log.Warning($"Failed to create mandatory objective {mandatoryObjectiveProto} for blood brothers");
            }
        }

        foreach (var objective in selectedObjectives)
        {
            var proto = MetaData(objective).EntityPrototype?.ID;
            if (proto == null)
                continue;

            var objective2 = _objectives.TryCreateObjective(mindId2, mind2, proto);
            if (objective2 != null)
            {
                _mindSystem.AddObjective(mindId2, mind2, objective2.Value);

                CopyObjectiveData(objective, objective2.Value, mindId1, mindId2);

                Log.Debug($"Created shared objective {proto} for both brothers");
            }
        }

        foreach (var objective in selectedObjectives)
        {
            _mindSystem.AddObjective(mindId1, mind1, objective);
        }

        Log.Info($"Generated {selectedObjectives.Count} shared objectives for blood brothers pair ({mindId1} and {mindId2})");
    }

    private void CopyObjectiveData(EntityUid sourceObjective, EntityUid targetObjective, EntityUid mindId1, EntityUid mindId2)
    {
        if (TryComp<TargetObjectiveComponent>(sourceObjective, out var sourceTarget)
            && sourceTarget.Target.HasValue && TryComp<TargetObjectiveComponent>(targetObjective, out var targetTarget))
        {
            _target.SetTarget(targetObjective, sourceTarget.Target.Value, targetTarget);
        }

        _sharedCondition.CopySharedConditionData(sourceObjective, targetObjective, mindId1, mindId2);
        _stealCondition.CopySharedStealConditionData(sourceObjective, targetObjective);
        _killCondition.CopySharedKillConditionData(sourceObjective, targetObjective);
    }

    private EntityUid? FindUnpairedBrother(EntityUid mindId, BloodBrotherRuleComponent component)
    {
        foreach (var otherMindId in component.BloodBrotherMinds)
        {
            if (otherMindId == mindId || component.BloodBrotherPairs.ContainsKey(mindId)
                || component.BloodBrotherPairs.ContainsKey(otherMindId)
                || component.BloodBrotherPairs.ContainsValue(mindId)
                || component.BloodBrotherPairs.ContainsValue(otherMindId))
                continue;

            return otherMindId;
        }

        return null;
    }

    private void CreateBloodBrotherPairInternal(EntityUid mindId1, EntityUid mindId2, Entity<BloodBrotherRuleComponent> component)
    {
        component.Comp.BloodBrotherPairs[mindId1] = mindId2;
        component.Comp.BloodBrotherPairs[mindId2] = mindId1;

        SetupBloodBrother(mindId1, mindId2, component.Comp);
        SetupBloodBrother(mindId2, mindId1, component.Comp);
    }

    private void SetupBloodBrother(EntityUid mindId, EntityUid brotherMindId, BloodBrotherRuleComponent component)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.OwnedEntity == null)
            return;

        _roleSystem.MindAddRole(mindId, component.BloodBrotherPrototypeId, silent: true);
        _roleSystem.MindHasRole<BloodBrotherRoleComponent>(mindId, out var bloodBrotherRole);

        if (bloodBrotherRole is not null)
        {
            EnsureComp<BloodBrotherComponent>(mindId, out var bloodBrotherComp);
            bloodBrotherComp.BrotherMind = brotherMindId;
            bloodBrotherComp.RequireBothAlive = component.RequireBothAlive;

            // Get brother info for RoleBriefingComponent
            var brotherName = GetBrotherName(brotherMindId);
            var brotherJob = GetBrotherJob(brotherMindId);
            var brotherBriefing = Loc.GetString("bloodbrother-role-brother-info",
                ("brotherName", brotherName),
                ("brotherJob", brotherJob));

            EnsureComp<RoleBriefingComponent>(bloodBrotherRole.Value.Owner, out var briefingComp);
            briefingComp.Briefing = brotherBriefing;

            if (component.GiveBriefing)
            {
                SendFullBriefing(mindId, brotherMindId, component);
            }
        }

        if (mind.OwnedEntity != null)
        {
            _npcFaction.RemoveFaction(mind.OwnedEntity.Value, component.NanoTrasenFaction, false);
            _npcFaction.AddFaction(mind.OwnedEntity.Value, component.SyndicateFaction);
        }
    }

    private void SendFullBriefing(EntityUid mindId, EntityUid brotherMindId, BloodBrotherRuleComponent component)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.OwnedEntity == null)
            return;

        var briefing = GenerateFullBriefing(mindId, brotherMindId, component);
        _antag.SendBriefing(mind.OwnedEntity.Value, briefing, BloodBrotherColor, component.GreetSoundNotification);
    }

    private string GenerateFullBriefing(EntityUid mindId, EntityUid brotherMindId, BloodBrotherRuleComponent component)
    {
        var sb = new StringBuilder();

        var issuerPrototype = _prototypeManager.Index(component.ObjectiveIssuers);
        var issuer = Loc.GetString(_random.Pick(issuerPrototype.Values));

        sb.AppendLine(Loc.GetString("bloodbrother-role-greeting",
            ("corporation", issuer ?? Loc.GetString("objective-issuer-unknown"))));

        var brotherName = GetBrotherName(brotherMindId);
        var brotherJob = GetBrotherJob(brotherMindId);

        sb.AppendLine("");
        sb.AppendLine(Loc.GetString("bloodbrother-role-brother-info",
            ("brotherName", brotherName),
            ("brotherJob", brotherJob)));

        sb.AppendLine("");
        if (component.RequireBothAlive)
        {
            sb.AppendLine("-> " + Loc.GetString("bloodbrother-role-both-alive-requirement"));
        }

        sb.AppendLine("-> " + Loc.GetString("bloodbrother-role-both-escape-requirement"));

        sb.AppendLine("-> " + Loc.GetString("bloodbrother-role-no-uplink-warning"));
        sb.AppendLine("");
        sb.AppendLine(Loc.GetString("bloodbrother-role-good-luck"));

        return sb.ToString();
    }

    private string GetBrotherName(EntityUid brotherMindId)
    {
        if (TryComp<MindComponent>(brotherMindId, out var brotherMind) && brotherMind.CharacterName != null)
            return brotherMind.CharacterName;

        return Loc.GetString("bloodbrother-unknown-name");
    }

    private string GetBrotherJob(EntityUid brotherMindId)
    {
        if (_jobs.MindTryGetJobName(brotherMindId) is { } jobName)
            return jobName;

        return Loc.GetString("bloodbrother-unknown-job");
    }
}
