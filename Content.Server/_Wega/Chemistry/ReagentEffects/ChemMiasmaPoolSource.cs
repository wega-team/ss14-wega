using Content.Shared.EntityEffects;
using Content.Server.Atmos.Rotting;
using Content.Server.Disease;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
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
                var rotting = args.EntityManager.System<RottingSystem>();
                string disease = rotting.RequestPoolDisease();

                var diseaseSystem = args.EntityManager.System<DiseaseSystem>();
                diseaseSystem.TryAddDisease(reagentArgs.TargetEntity, disease);
            }
        }
    }
}
