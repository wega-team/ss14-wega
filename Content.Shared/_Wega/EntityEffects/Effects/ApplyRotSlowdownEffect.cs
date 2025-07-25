using Robust.Shared.Prototypes;
using Content.Shared.Atmos.Rotting;
using JetBrains.Annotations;
using System.Globalization;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// It slows down the entity's decay process by the specified coefficient and time.
/// </summary>
[UsedImplicitly]
public sealed partial class ApplyRotSlowdown : EntityEffect
{
    /// <summary>
    /// Decay slowdown multiplier (0.5 = decay 2 times slower).
    /// </summary>
    [DataField]
    public float Factor { get; private set; } = 0.5f;

    /// <summary>
    /// Duration of the effect in seconds.
    /// </summary>
    [DataField]
    public float Duration { get; private set; } = 60f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-apply-rot-slowdown",
            ("factor", Factor.ToString("0.00", CultureInfo.InvariantCulture)),
            ("duration", Duration));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Scale != 1f)
                return;
        }

        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRottingSystem>();
        sys.ApplyRotSlowdown(args.TargetEntity, Factor, TimeSpan.FromSeconds(Duration));
    }
}
