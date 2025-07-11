using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSlimeVisualSystem))]
public sealed partial class SlimeVisualsComponent : Component
{
    [DataField]
    public ProtoId<EntityPrototype>? DefaultVisuals;

    [DataField]
    public Dictionary<SlimeType, ProtoId<EntityPrototype>> TypeVisuals = new();
}
