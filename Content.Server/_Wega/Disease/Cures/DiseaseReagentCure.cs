using Content.Shared.Disease;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Body.Components;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    /// Cures the disease if a certain amount of reagent
    /// is in the host's chemstream.
    /// </summary>
    public sealed partial class DiseaseReagentCure : DiseaseCure
    {
        [DataField("min")]
        public FixedPoint2 Min = 5;

        [DataField("reagent")]
        public string? Reagent;

        public override bool Cure(DiseaseEffectArgs args)
        {
            // Ensure EntityManager is used to access components
            if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.DiseasedEntity, out var bloodstream))
                return false;

            if (bloodstream.BloodSolution == null)
                return false;

            var solutionComponent = args.EntityManager.GetComponent<SolutionComponent>(bloodstream.BloodSolution.Value);
            var solution = solutionComponent.Solution;  // Access the Solution object

            var quant = FixedPoint2.Zero;
            if (Reagent != null)
            {
                var reagentId = new ReagentId(Reagent, null);

                if (solution.ContainsReagent(reagentId))
                {
                    quant = solution.GetReagentQuantity(reagentId);
                }
            }

            return quant >= Min;
        }

        public override string CureText()
        {
            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
            if (Reagent == null || !prototypeMan.TryIndex<ReagentPrototype>(Reagent, out var reagentProt))
                return string.Empty;
            return (Loc.GetString("diagnoser-cure-reagent", ("units", Min), ("reagent", reagentProt.LocalizedName)));
        }
    }
}
