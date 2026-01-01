using System.Globalization;
using Content.Shared.Atmos.Rotting;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Slows down the entity's decay process by the specified coefficient and time.
/// The duration of the effect is equal to <see cref="ApplyRotSlowdown.Duration"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ApplyRotSlowdownEntityEffectSystem : EntityEffectSystem<RottingComponent, ApplyRotSlowdown>
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

    protected override void Effect(Entity<RottingComponent> entity, ref EntityEffectEvent<ApplyRotSlowdown> args)
    {
        var duration = args.Effect.Duration * args.Scale;
        var factor = args.Effect.Factor;

        _rotting.ApplyRotSlowdown(entity, factor, TimeSpan.FromSeconds(duration));
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ApplyRotSlowdown : EntityEffectBase<ApplyRotSlowdown>
{
    /// <summary>
    ///     Decay slowdown multiplier (0.5 = decay 2 times slower).
    /// </summary>
    [DataField]
    public float Factor { get; private set; } = 0.5f;

    /// <summary>
    ///     Duration of the effect in seconds.
    /// </summary>
    [DataField]
    public float Duration { get; private set; } = 60f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-apply-rot-slowdown",
            ("factor", Factor.ToString("0.00", CultureInfo.InvariantCulture)),
            ("duration", Duration));
}
