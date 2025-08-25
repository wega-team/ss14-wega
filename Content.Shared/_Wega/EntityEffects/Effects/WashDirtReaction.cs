using Content.Shared.DirtVisuals;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    public sealed partial class WashDirtReaction : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-wash-dirt-reaction");

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent<DirtableComponent>(args.TargetEntity, out var dirtable)
                || dirtable.CurrentDirtLevel <= 0)
                return;

            var amount = args is EntityEffectReagentArgs reagentArgs
                ? (float)reagentArgs.Quantity
                : 5f;

            var dirtSystem = args.EntityManager.EntitySysManager.GetEntitySystem<SharedDirtSystem>();
            dirtSystem.CleanDirt(args.TargetEntity, amount);
        }
    }
}