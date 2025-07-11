using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Content.Shared.Genetics;

namespace Content.Shared.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class ChemMutateDna : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-mutate-dna");

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is not EntityEffectReagentArgs reagentArgs)
                return;

            var ev = new MutateDnaAttemptEvent();
            args.EntityManager.EventBus.RaiseLocalEvent(reagentArgs.TargetEntity, ev, false);
        }
    }
}
