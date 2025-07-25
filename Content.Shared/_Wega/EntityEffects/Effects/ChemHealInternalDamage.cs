using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery;

namespace Content.Shared.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class ChemHealInternalDamage : EntityEffect
    {
        [DataField("healChance")]
        public float HealChance = 0.1f;

        [DataField("damageTypes")]
        public List<ProtoId<InternalDamagePrototype>>? DamageTypes = null;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-heal-internal-damage",
                ("chance", HealChance));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is not EntityEffectReagentArgs reagentArgs)
                return;

            var target = reagentArgs.TargetEntity;
            if (!args.EntityManager.TryGetComponent<OperatedComponent>(target, out var operated))
                return;

            var random = IoCManager.Resolve<IRobustRandom>();
            var scaledChance = HealChance * reagentArgs.Scale.Float();

            foreach (var (damageId, bodyParts) in operated.InternalDamages)
            {
                if (DamageTypes != null && !DamageTypes.Contains(damageId))
                    continue;

                if (!random.Prob(scaledChance))
                    continue;

                if (bodyParts.Count > 0)
                {
                    var healedPart = random.Pick(bodyParts);
                    bodyParts.Remove(healedPart);
                }

                if (bodyParts.Count == 0)
                {
                    operated.InternalDamages.Remove(damageId);
                }
            }
        }
    }
}