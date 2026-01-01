using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes the specified disease on the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemCauseDiseaseEntityEffectSystem : EntityEffectSystem<DiseaseCarrierComponent, ChemCauseDisease>
{
    protected override void Effect(Entity<DiseaseCarrierComponent> entity, ref EntityEffectEvent<ChemCauseDisease> args)
    {
        var diseaseSys = EntityManager.System<SharedDiseaseSystem>();

        var disease = args.Effect.Disease;
        diseaseSys.TryAddDisease(entity, disease);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemCauseDisease : EntityEffectBase<ChemCauseDisease>
{
    /// <summary>
    ///     The disease to add.
    /// </summary>
    [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>), required: true)]
    public string Disease = default!;

    /// <summary>
    ///     Chance it has each tick to cause disease, between 0 and 1
    /// </summary>
    [DataField("causeChance")]
    public float CauseChance = 0.15f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-disease",
            ("chance", CauseChance),
            ("disease", Disease));
}
