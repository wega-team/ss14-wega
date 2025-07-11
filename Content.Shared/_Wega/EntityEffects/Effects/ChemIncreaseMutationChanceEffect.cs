using Content.Shared.EntityEffects;
using Content.Shared.Xenobiology.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemIncreaseMutationChanceEffect : EntityEffect
    {
        [DataField]
        public float FixedIncrease = 0.12f;

        [DataField]
        public float MaxMutationChance = 0.95f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-increase-mutation-chance",
                ("increase", (int)(FixedIncrease * 100)),
                ("max", (int)(MaxMutationChance * 100)));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent<SlimeGrowthComponent>(args.TargetEntity, out var growth))
                return;

            growth.MutationChance = Math.Min(growth.MutationChance + FixedIncrease, MaxMutationChance);

            args.EntityManager.Dirty(args.TargetEntity, growth);
        }
    }
}