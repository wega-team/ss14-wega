using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

[RegisterComponent]
public sealed partial class InternalStorageComponent : Component
{
    /// <summary>
    /// Container for items in the mouth (max. 1 item)
    /// </summary>
    [ViewVariables]
    public ContainerSlot ToothContainer = default!;

    /// <summary>
    /// Container for items in the head (max. 1 item)
    /// </summary>
    [ViewVariables]
    public ContainerSlot HeadContainer = default!;

    /// <summary>
    /// Container for items in the torso (max. 3 items)
    /// </summary>
    [ViewVariables]
    public Container BodyContainer = default!;

    /// <summary>
    /// Entity to use for the tooth implant action.
    /// </summary>
    [DataField("toothImplantAction")]
    public EntityUid? ToothImplantActionEntity;

    /// <summary>
    /// The maximum size of objects for the head
    /// </summary>
    [DataField("headMaxSize")]
    public ProtoId<ItemSizePrototype> HeadMaxSize = "Tiny";

    /// <summary>
    /// Maximum size of objects for the torso
    /// </summary>
    [DataField("bodyMaxSize")]
    public ProtoId<ItemSizePrototype> BodyMaxSize = "Small";
}
