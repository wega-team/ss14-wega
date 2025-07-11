using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Disease;

namespace Content.Shared.Chemistry.ReagentEffects
{
    /// <summary>
    /// The miasma system rotates between 1 disease at a time.
    /// This gives all entities the disease the miasme system is currently on.
    /// For things ingested by one person, you probably want ChemCauseRandomDisease instead.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemAtmosPoolSource : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-atmos-pool-source");

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs && reagentArgs.Scale == 1f)
            {
                var rotting = args.EntityManager.System<SharedRottingSystem>();
                string disease = rotting.RequestPoolDisease();

                var diseaseSystem = args.EntityManager.System<SharedDiseaseSystem>();
                diseaseSystem.TryAddDisease(reagentArgs.TargetEntity, disease);
            }
        }
    }
}
