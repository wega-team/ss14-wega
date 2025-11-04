using Content.Shared.Atmos.Rotting;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Gives the entity the current disease from the miasma system.
/// The miasma system rotates between 1 disease at a time.
/// For things ingested by one person, you probably want ChemCauseRandomDisease instead.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemAtmosPoolSourceEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, ChemAtmosPoolSource>
{
    [Dependency] private readonly SharedRottingSystem _rotting = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<ChemAtmosPoolSource> args)
    {
        var diseaseSys = EntityManager.System<SharedDiseaseSystem>();

        var disease = _rotting.RequestPoolDisease();
        diseaseSys.TryAddDisease(entity, disease);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemAtmosPoolSource : EntityEffectBase<ChemAtmosPoolSource>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-atmos-pool-source");
}
