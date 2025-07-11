using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;

namespace Content.Shared.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class HolyPurification : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return Loc.GetString("This reagent has a deconverts forcibly recruited cultists.");
        }

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (reagentArgs.Scale != 1f)
                    return;

                // Add here new cults deconverted with the help of holy water if necessary
                if (args.EntityManager.HasComponent<BloodCultistComponent>(reagentArgs.TargetEntity))
                {
                    var bloodCultSystem = args.EntityManager.System<SharedBloodCultSystem>();
                    bloodCultSystem.CultistDeconvertation(reagentArgs.TargetEntity);
                }
            }
        }
    }
}
