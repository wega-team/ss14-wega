using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class VeilCultRitualObjectiveSystem : EntitySystem
{
    [Dependency] private VeilCultRuleSystem _veilCult = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VeilCultRitualObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, VeilCultRitualObjectiveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var cult = _veilCult.GetActiveRule();
        args.Progress = 0f;

        if (cult != null)
        {
            if (cult.RitualGoing)
            {
                args.Progress = 0.5f;
            }
            var condition = cult.VeilCultWinCondition.ToList();
            if (condition.Contains(VeilCultWinType.GodCalled))
            {
                args.Progress = 1f;
            }
        }
    }
}
