// Developed by Nox project.
// Author: KloopRe

using System.Linq;
using Content.Server._Modifications.Disease.Systems;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;

namespace Content.Server._Modifications.Disease.Effects;

public sealed partial class CauseDiseaseEntityEffectsSystem : EntityEffectSystem<SolutionManagerComponent, CauseDiseaseEffect>
{
    [Dependency] private DiseaseSystem _diseaseSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionManagerComponent> entity, ref EntityEffectEvent<CauseDiseaseEffect> args)
    {
        var container = new Entity<SolutionManagerComponent?>(entity.Owner, entity.Comp);

        Entity<SolutionComponent>? solutionEntity = null;
        if (!_solutionContainer.ResolveSolution(container, args.Effect.Solution, ref solutionEntity, out var solution)
            || solution == null)
        {
            return;
        }

        TryInfectFromSolution(entity.Owner, solution);
    }

    private bool TryInfectFromSolution(EntityUid target, Solution solution)
    {
        foreach (var (reagentId, _) in solution.Contents)
        {
            var dataList = reagentId.Data;

            if (dataList == null)
                continue;

            var candidate = dataList.OfType<DiseaseData>().FirstOrDefault();
            if (candidate == null)
                continue;

            var infectionData = (DiseaseData)candidate.CloneForInfection();
            var infectivity = _diseaseSystem.CalcInfectionInfectivity(infectionData);

            _diseaseSystem.ProbInfect(candidate, target, infectivity: infectivity, ignoreResistance: true);
            return true;
        }

        return false;
    }
}
