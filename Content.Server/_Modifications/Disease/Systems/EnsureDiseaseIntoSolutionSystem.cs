// Developed by Nox project.
// Author: KloopRe

using Content.Server._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;


namespace Content.Server._Modifications.Disease.Systems;

public sealed partial class EnsureDiseaseIntoSolutionSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private DiseaseDiagnoserSystem _diseaseDiagnoser = default!;
    [Dependency] private DiseaseSystem _diseaseSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsureDiseaseIntoSolutionComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedSolutionContainerSystem)]);
    }

    private void OnMapInit(Entity<EnsureDiseaseIntoSolutionComponent> ent, ref MapInitEvent args)
    {
        Apply(ent);
    }

    private void Apply(Entity<EnsureDiseaseIntoSolutionComponent> ent)
    {
        if (ent.Comp.ReagentAdded)
            return;

        ent.Comp.ReagentAdded = true;

        if (!TryComp<SolutionManagerComponent>(ent.Owner, out var solutionManager))
            return;

        if (!TryComp<DrawableSolutionComponent>(ent.Owner, out var drawable))
            return;

        var entWrapper = new Entity<DrawableSolutionComponent?, SolutionManagerComponent?>(ent.Owner, drawable, solutionManager);

        if (!_solutionContainer.TryGetDrawableSolution(entWrapper, out Entity<SolutionComponent>? solutionEntity, out Solution? solution))
            return;

        if (solutionEntity == null || solution == null)
            return;

        _solutionContainer.TryAddReagent(solutionEntity.Value, _diseaseDiagnoser.Reagent, solution.MaxVolume, out _);

        for (var i = 0; i < solution.Contents.Count; i++)
        {
            var reagent = solution.Contents[i];

            if (reagent.Reagent.Prototype != _diseaseDiagnoser.Reagent)
                continue;

            var reagentData = reagent.Reagent.Data != null
                ? new List<ReagentData>(reagent.Reagent.Data)
                : new List<ReagentData>();

            reagentData.RemoveAll(x => x is DiseaseData);

            var diseaseData = (DiseaseData)(ent.Comp.Data?.Clone() ?? _diseaseSystem.CreateNewDisease());
            reagentData.Add(diseaseData);

            solution.Contents[i] = new ReagentQuantity(new ReagentId(_diseaseDiagnoser.Reagent, reagentData), reagent.Quantity);
        }

        _solutionContainer.UpdateChemicals(solutionEntity.Value);
    }


}
