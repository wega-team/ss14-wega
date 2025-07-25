using Content.Shared.EntityEffects;
using Content.Shared.Xenobiology.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemReinforcedExtractsEffect : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-increase-mutation-chance");

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent<SlimeGrowthComponent>(args.TargetEntity, out var growth))
                return;

            if (growth.Reinforced)
                return;

            growth.Reinforced = true;

            args.EntityManager.Dirty(args.TargetEntity, growth);
        }
    }
}