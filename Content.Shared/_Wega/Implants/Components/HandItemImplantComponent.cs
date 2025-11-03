using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HandItemImplantComponent : Component
{
    [DataField]
    public List<HandItemImplantSlot> Items = new();

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [DataField]
    public string ContainerName = "itemImplant";
    [DataField]
    public Container? Container;
}

[DataRecord]
public partial struct HandItemImplantSlot
{
    [DataField("hand")]
    public string HandId;

    [DataField("prototype")]
    public string ItemPrototype;
    public EntityUid? ItemEntity;

    [DataField("toggleAction")]
    public string ToggleActionPrototype;
    public EntityUid? ToggleActionEntity;

    [DataField]
    public EntityUid? ImplantEntity;

    public HandItemImplantSlot(string handId, string itemPrototype, string toggleAction, EntityUid? implant = null)
    {
        HandId = handId;
        ItemPrototype = itemPrototype;
        ToggleActionPrototype = toggleAction;
        ImplantEntity = implant;
    }
}
