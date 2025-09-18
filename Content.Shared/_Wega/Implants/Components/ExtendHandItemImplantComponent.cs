using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExtendHandItemImplantComponent : Component
{
    [DataField]
    public List<HandItemImplantSlot> Items = new List<HandItemImplantSlot>();
}
