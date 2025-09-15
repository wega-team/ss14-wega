using Content.Shared.Body.Part;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Implants.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class HandItemImplantComponent : Component
    {
        [DataField("hand", required: true)]
        public string HandId;

        [DataField]
        public string ToggleActionPrototype;
        public EntityUid? ToggleActionEntity;

        [DataField]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

        [DataField(required: true)]
        public string ItemPrototype;
        [DataField]
        public string ContainerName = "itemImplant";
        [DataField]
        public ContainerSlot? Container;
        [DataField]
        public EntityUid? ItemEntity;
    }
}

[Serializable]
[DataRecord]
public partial struct HandItemImplantSlot
{
    public string HandId;

    public string ToggleActionPrototype;
    public EntityUid? ToggleActionEntity;

    public HandItemImplantSlot(string id, BodyPartType type)
    {
        Id = id;
        Type = type;
    }
};
