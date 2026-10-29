using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SpawnOnDeleteComponent : Component
    {
        /// <summary>
        /// Entity prototype to spawn.
        /// </summary>
        [DataField(required: true)]
        public EntProtoId Prototype = string.Empty;
    }
}
