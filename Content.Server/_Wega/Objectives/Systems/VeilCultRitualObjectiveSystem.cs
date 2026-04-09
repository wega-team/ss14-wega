using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class VeilCultRitualObjectiveSystem : EntitySystem
{
    [Dependency] private readonly VeilCultRuleSystem _veilCult = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VeilCultRitualObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, VeilCultRitualObjectiveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var cult = _veilCult.GetActiveRule();
        if (cult == null || !cult.RitualStage)
        {
            args.Progress = 0f;
            return;
        }

        var condition = cult.VeilCultWinCondition.ToList();
        if (condition.Contains(VeilCultWinType.GodCalled))
        {
            args.Progress = 1f;
        }
        else
        {
            args.Progress = 0.5f;
        }
    }
}
