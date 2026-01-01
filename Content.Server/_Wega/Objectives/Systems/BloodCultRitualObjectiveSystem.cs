using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class BloodCultRitualObjectiveSystem : EntitySystem
{
    [Dependency] private readonly BloodCultRuleSystem _bloodCult = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultRitualObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodCultRitualObjectiveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var cult = _bloodCult.GetActiveRule();
        if (cult == null || !cult.RitualStage)
        {
            args.Progress = 0f;
            return;
        }

        var condition = cult.BloodCultWinCondition.ToList();
        if (condition.Contains(BloodCultWinType.GodCalled))
        {
            args.Progress = 1f;
        }
        else
        {
            args.Progress = 0.5f;
        }
    }
}
