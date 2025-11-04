using Content.Shared.DirtVisuals;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Cleans dirt from dirtable entities.
/// The cleaning amount is equal to <see cref="WashDirt.CleaningAmount"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class WashDirtEntityEffectSystem : EntityEffectSystem<DirtableComponent, WashDirt>
{
    [Dependency] private readonly SharedDirtSystem _dirt = default!;

    protected override void Effect(Entity<DirtableComponent> entity, ref EntityEffectEvent<WashDirt> args)
    {
        if (entity.Comp.CurrentDirtLevel <= 0)
            return;

        var cleaningAmount = args.Effect.CleaningAmount * args.Scale;
        _dirt.CleanDirt(entity, cleaningAmount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class WashDirt : EntityEffectBase<WashDirt>
{
    /// <summary>
    ///     Amount of dirt to clean.
    /// </summary>
    [DataField]
    public float CleaningAmount = 5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-wash-dirt-reaction");
}
