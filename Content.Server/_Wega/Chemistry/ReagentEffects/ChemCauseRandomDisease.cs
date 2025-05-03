using Content.Shared.EntityEffects;
using Content.Server.Disease;
using Content.Shared.Disease.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Causes a random disease from a list, if the user is not already diseased.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemCauseRandomDisease : EntityEffect
    {
        /// <summary>
        /// A disease to choose from.
        /// </summary>
        [DataField("diseases", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> Diseases = default!;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var diseasesList = string.Join(", ", Diseases);
            return Loc.GetString("reagent-effect-guidebook-cause-random-disease",
                ("diseases", diseasesList));
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (args.EntityManager.TryGetComponent<DiseasedComponent>(reagentArgs.TargetEntity, out var diseased))
                    return;

                if (reagentArgs.Scale != 1f)
                    return;

                var random = IoCManager.Resolve<IRobustRandom>();
                var randomDisease = random.Pick(Diseases);

                var diseaseSystem = args.EntityManager.System<DiseaseSystem>();
                diseaseSystem.TryAddDisease(reagentArgs.TargetEntity, randomDisease);
            }
        }
    }
}
