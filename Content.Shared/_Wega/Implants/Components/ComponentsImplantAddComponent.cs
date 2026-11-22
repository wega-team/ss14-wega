using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComponentsImplantAddComponent : Component
{
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();

    [DataField("implantKey")]
    public string? ImplantKey;
}
