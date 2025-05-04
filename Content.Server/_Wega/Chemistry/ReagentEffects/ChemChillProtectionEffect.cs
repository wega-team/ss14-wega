using Content.Server.Temperature.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemChillProtectionEffect : EntityEffect
    {
        /// <summary>
        /// Heating coefficient to apply (0.001 makes entity heat up very slowly)
        /// </summary>
        [DataField]
        public float HeatingCoefficient = 0.001f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-temperature-fire-protection",
                ("heating", HeatingCoefficient));

        public override void Effect(EntityEffectBaseArgs args)
        {
            var entityManager = args.EntityManager;
            var uid = args.TargetEntity;

            if (!entityManager.TryGetComponent(uid, out TemperatureProtectionComponent? tempProtection))
                return;

            tempProtection.HeatingCoefficient = HeatingCoefficient;
        }
    }
}
