using Content.Shared.EntityEffects;
using Content.Shared.Xenobiology.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemStabilizeMutationChanceEffect : EntityEffect
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        [DataField]
        public float MinReduction = 0.15f;

        [DataField]
        public float MaxReduction = 0.45f;

        [DataField]
        public float MinMutationChance = 0.05f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-stabilize-mutation",
                ("minReduction", (int)(MinReduction * 100)),
                ("maxReduction", (int)(MaxReduction * 100)),
                ("min", (int)(MinMutationChance * 100)));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent<SlimeGrowthComponent>(args.TargetEntity, out var growth)
                || growth.Stabilized)
                return;

            var reductionPercent = _random.NextFloat(MinReduction, MaxReduction);
            var newChance = growth.MutationChance * (1 - reductionPercent);
            growth.MutationChance = Math.Max(newChance, MinMutationChance);
            growth.Stabilized = true;

            args.EntityManager.Dirty(args.TargetEntity, growth);
        }
    }
}
