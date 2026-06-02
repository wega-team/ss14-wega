using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ninja.Components;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Assigns ninja objectives that require crew targets (kill, frame, intimidate)
/// after crew has spawned, since PrePlayerSpawn fires before PickRandomPerson can find anyone.
/// </summary>
public sealed class NinjaDelayedObjectivesSystem : GameRuleSystem<NinjaDelayedObjectivesComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned);
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var delayed, out _))
        {
            if (!TryComp<AntagSelectionComponent>(uid, out var selection))
                continue;

            foreach (var minds in selection.AssignedMinds.Values)
            foreach (var (mindId, _) in minds)
            {
                if (!TryComp<MindComponent>(mindId, out var mind))
                    continue;

                foreach (var id in delayed.FixedObjectives)
                {
                    _mind.TryAddObjective(mindId, mind, id);
                }

                var difficulty = 0f;
                foreach (var set in delayed.Sets)
                {
                    if (!_random.Prob(set.Prob))
                        continue;

                    for (var pick = 0; pick < set.MaxPicks && delayed.MaxDifficulty > difficulty; pick++)
                    {
                        var remaining = delayed.MaxDifficulty - difficulty;
                        if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remaining) is not { } objective)
                            continue;

                        _mind.AddObjective(mindId, mind, objective);
                        difficulty += Comp<ObjectiveComponent>(objective).Difficulty;
                    }
                }
            }
        }
    }
}
