using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult.UI;

[Serializable, NetSerializable]
public enum VeilAltarUiKey : byte
{
    Key
}

// Events
[Serializable, NetSerializable]
public sealed partial class VeilAltarSelectEnergyMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class VeilAltarSelectOfferMessage : BoundUserInterfaceMessage
{
}
