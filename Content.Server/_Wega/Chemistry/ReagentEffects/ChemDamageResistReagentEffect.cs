using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemDamageResistReagentEffect : EntityEffect
    {
        [DataField("damageTypes", required: true)]
        public List<string> DamageTypes;

        [DataField]
        public float ResistFactor = 0.2f;

        [DataField]
        public float Duration = 30f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var types = string.Join(", ", DamageTypes);
            return Loc.GetString("reagent-effect-guidebook-damage-resist",
                ("types", types),
                ("resist", (int)((1 - ResistFactor) * 100)),
                ("duration", Duration));
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            var gameTiming = IoCManager.Resolve<IGameTiming>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var resistComp = args.EntityManager.EnsureComponent<DamageResistComponent>(args.TargetEntity);
            foreach (var damageTypeId in DamageTypes)
            {
                if (!prototypeManager.TryIndex<DamageTypePrototype>(damageTypeId, out var damageType))
                    continue;

                resistComp.Resistances[damageType] = (
                    ResistFactor,
                    gameTiming.CurTime + TimeSpan.FromSeconds(Duration)
                );
            }

            args.EntityManager.Dirty(args.TargetEntity, resistComp);
        }
    }
}