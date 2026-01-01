using Content.Shared.Disease.Components;
using Content.Shared.Disease.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Cures a disease with a chance each tick.
/// The cure chance is equal to <see cref="ChemCureDisease.CureChance"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemCureDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, ChemCureDisease>
{
    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<ChemCureDisease> args)
    {
        var cureChance = args.Effect.CureChance * args.Scale;

        var ev = new CureDiseaseAttemptEvent(cureChance);
        RaiseLocalEvent(entity, ev, false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemCureDisease : EntityEffectBase<ChemCureDisease>
{
    /// <summary>
    ///     Chance it has each tick to cure a disease, between 0 and 1
    /// </summary>
    [DataField("cureChance")]
    public float CureChance = 0.15f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cure-disease",
            ("chance", CureChance));
}
