using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Body.Components;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Adds or removes reagents from the host's chemstream.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class DiseaseAdjustReagent : DiseaseEffect
    {
        /// <summary>
        /// The reagent ID to add or remove.
        /// </summary>
        [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string? Reagent = null;

        [DataField("amount", required: true)]
        public FixedPoint2 Amount = default!;

        public override void Effect(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.HasComponent<BloodstreamComponent>(args.DiseasedEntity))
                return;

            var solutionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedSolutionContainerSystem>();
            if (Reagent is null || !solutionSys.TryGetSolution(args.DiseasedEntity, BloodstreamComponent.DefaultBloodSolutionName, out var solutionEntity, out var solution) || solutionEntity is null)
                return;

            var reagentId = new ReagentId(Reagent, new List<ReagentData>());
            var reagentQuantity = new ReagentQuantity(reagentId, Amount);

            FixedPoint2 acceptedQuantity;

            if (Amount < 0 && solution.ContainsReagent(reagentId))
                solutionSys.RemoveReagent(solutionEntity.Value, reagentQuantity);

            if (Amount > 0)
                solutionSys.TryAddReagent(solutionEntity.Value, reagentId, Amount, out acceptedQuantity);
        }
    }
}
