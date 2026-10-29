using Robust.Shared.Prototypes;

namespace Content.Shared.Disease.Components
{
    /// <summary>
    /// For shared behavior between both disease machines
    /// </summary>
    [RegisterComponent]
    public sealed partial class DiseaseMachineComponent : Component
    {
        [DataField("delay")]
        public float Delay = 5f;
        /// <summary>
        /// How much time we've accumulated processing
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;
        /// <summary>
        /// The disease prototype currently being diagnosed
        /// </summary>
        [ViewVariables]
        public DiseasePrototype? Disease;
        /// <summary>
        /// What the machine will spawn
        /// </summary>
        [DataField("machineOutput", required: true)]
        public EntProtoId MachineOutput = default!;
    }
}
