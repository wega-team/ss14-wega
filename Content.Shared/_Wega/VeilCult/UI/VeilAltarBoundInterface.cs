using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult.UI;

[Serializable, NetSerializable]
public enum VeilAltarUiKey : byte
{
    Key
}

// Events
[Serializable, NetSerializable]
public sealed class VeilAltarSelectEnergyMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class VeilAltarSelectOfferMessage : BoundUserInterfaceMessage
{
}
