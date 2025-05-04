using Content.Shared.EntityEffects;
using Content.Server.Disease;
using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemCauseDisease : EntityEffect
    {
        /// <summary>
        /// Chance it has each tick to cause disease, between 0 and 1
        /// </summary>
        [DataField("causeChance")]
        public float CauseChance = 0.15f;

        /// <summary>
        /// The disease to add.
        /// </summary>
        [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>), required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Disease = default!;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-cause-disease",
                ("chance", CauseChance),
                ("disease", Disease));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (reagentArgs.Scale != 1f)
                    return;

                var diseaseSystem = args.EntityManager.System<DiseaseSystem>();
                diseaseSystem.TryAddDisease(reagentArgs.TargetEntity, Disease);
            }
        }
    }
}
