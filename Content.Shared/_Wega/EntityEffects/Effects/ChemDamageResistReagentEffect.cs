using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Applies damage resistance to specified damage types for a duration.
/// The duration is equal to <see cref="ChemDamageResist.Duration"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemDamageResistEntityEffectSystem : EntityEffectSystem<DamageResistComponent, ChemDamageResist>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void Effect(Entity<DamageResistComponent> entity, ref EntityEffectEvent<ChemDamageResist> args)
    {
        var duration = args.Effect.Duration * args.Scale;
        var resistFactor = args.Effect.ResistFactor;

        foreach (var damageTypeId in args.Effect.DamageTypes)
        {
            if (!_prototype.TryIndex<DamageTypePrototype>(damageTypeId, out var damageType))
                continue;

            entity.Comp.Resistances[damageType] = (
                resistFactor,
                _gameTiming.CurTime + TimeSpan.FromSeconds(duration)
            );
        }

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemDamageResist : EntityEffectBase<ChemDamageResist>
{
    /// <summary>
    ///     Damage types to apply resistance to.
    /// </summary>
    [DataField("damageTypes", required: true)]
    public List<string> DamageTypes = default!;

    /// <summary>
    ///     Resistance factor (0.2 = 20% resistance).
    /// </summary>
    [DataField]
    public float ResistFactor = 0.2f;

    /// <summary>
    ///     Duration of the effect in seconds.
    /// </summary>
    [DataField]
    public float Duration = 30f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var types = string.Join(", ", DamageTypes);
        return Loc.GetString("reagent-effect-guidebook-damage-resist",
            ("types", types),
            ("resist", (int)(ResistFactor * 100)),
            ("duration", Duration));
    }
}
