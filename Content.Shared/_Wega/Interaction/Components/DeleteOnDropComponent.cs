using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class DeleteOnDropComponent : Component
    {
        [DataField("deleteOnDrop")]
        public bool DeleteOnDrop = true;
    }
}
