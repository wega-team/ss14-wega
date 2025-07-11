using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Content.Shared.Disease.Events;

namespace Content.Shared.Chemistry.ReagentEffects
{
    /// <summary>
    /// Cures a disease with a chance each tick.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemCureDisease : EntityEffect
    {
        /// <summary>
        /// Chance it has each tick to cure a disease, between 0 and 1
        /// </summary>
        [DataField("cureChance")]
        public float CureChance = 0.15f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-cure-disease",
                ("chance", CureChance));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                float cureChance = CureChance * reagentArgs.Scale.Float();

                var ev = new CureDiseaseAttemptEvent(cureChance);
                args.EntityManager.EventBus.RaiseLocalEvent(reagentArgs.TargetEntity, ev, false);
            }
        }
    }
}
