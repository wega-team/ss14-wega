using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes a random disease from a list, if the user is not already diseased.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemCauseRandomDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, ChemCauseRandomDisease>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<ChemCauseRandomDisease> args)
    {
        if (entity.Comp.Diseases.Count > 0)
            return;

        var diseaseSys = EntityManager.System<SharedDiseaseSystem>();

        var randomDisease = _random.Pick(args.Effect.Diseases);
        diseaseSys.TryAddDisease(entity, randomDisease);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemCauseRandomDisease : EntityEffectBase<ChemCauseRandomDisease>
{
    /// <summary>
    ///     A disease to choose from.
    /// </summary>
    [DataField("diseases", required: true)]
    public List<string> Diseases = default!;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var diseasesList = string.Join(", ", Diseases);
        return Loc.GetString("reagent-effect-guidebook-cause-random-disease",
            ("diseases", diseasesList));
    }
}
