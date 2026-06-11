using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult.UI;

[Serializable, NetSerializable]
public sealed partial class EnchantSelectionState : BoundUserInterfaceState
{
    public readonly List<EntProtoId> AvailableEnchants = new();

    public EnchantSelectionState(List<EntProtoId> availableEnchants)
    {
        AvailableEnchants = availableEnchants;
    }
}

[Serializable, NetSerializable]
public sealed partial class EnchantSelectedMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId EnchantId;

    public EnchantSelectedMessage(EntProtoId enchantId)
    {
        EnchantId = enchantId;
    }
}

[Serializable, NetSerializable]
public enum EnchantUiKey : byte
{
    Key
}
