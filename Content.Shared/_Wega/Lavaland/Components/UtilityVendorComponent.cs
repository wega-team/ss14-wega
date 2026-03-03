using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
[Access(typeof(UtilityVendorSystem))]
public sealed partial class UtilityVendorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<ProtoId<UtilityVendorCategoryPrototype>> Categories { get; private set; } = new();

    [DataField]
    public ItemSlot CardSlot = new();

    [DataField]
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
}
