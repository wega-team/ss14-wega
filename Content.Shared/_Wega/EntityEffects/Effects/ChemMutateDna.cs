using Content.Shared.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Attempts to mutate the entity's DNA.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemMutateDnaEntityEffectSystem : EntityEffectSystem<DnaModifierComponent, ChemMutateDna>
{
    protected override void Effect(Entity<DnaModifierComponent> entity, ref EntityEffectEvent<ChemMutateDna> args)
    {
        var ev = new MutateDnaAttemptEvent();
        RaiseLocalEvent(entity, ev, false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemMutateDna : EntityEffectBase<ChemMutateDna>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-mutate-dna");
}
