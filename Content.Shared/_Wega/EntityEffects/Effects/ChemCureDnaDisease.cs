using Content.Shared.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Cures a DNA disease with a chance each tick.
/// The cure chance is equal to <see cref="ChemCureDnaDisease.CureChance"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemCureDnaDiseaseEntityEffectSystem : EntityEffectSystem<DnaModifierComponent, ChemCureDnaDisease>
{
    protected override void Effect(Entity<DnaModifierComponent> entity, ref EntityEffectEvent<ChemCureDnaDisease> args)
    {
        var cureChance = args.Effect.CureChance * args.Scale;

        var ev = new CureDnaDiseaseAttemptEvent(cureChance);
        RaiseLocalEvent(entity, ev, false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemCureDnaDisease : EntityEffectBase<ChemCureDnaDisease>
{
    /// <summary>
    ///     Chance it has each tick to cure a DNA disease, between 0 and 1
    /// </summary>
    [DataField("cureChance")]
    public float CureChance = 0.10f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cure-dna-disease",
            ("chance", CureChance));
}
