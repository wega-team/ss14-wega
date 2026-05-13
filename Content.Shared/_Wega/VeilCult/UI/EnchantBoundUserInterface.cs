using Content.Shared.Veil.Cult.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Veil.Cult.UI;

[Serializable, NetSerializable]
public sealed class EnchantSelectionState : BoundUserInterfaceState
{
    public readonly List<EntProtoId> AvailableEnchants = new();

    public EnchantSelectionState(List<EntProtoId> availableEnchants)
    {
        AvailableEnchants = availableEnchants;
    }
}

[Serializable, NetSerializable]
public sealed class EnchantSelectedMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity User;
    public readonly EntProtoId EnchantId;

    public EnchantSelectedMessage(NetEntity user, EntProtoId enchantId)
    {
        User = user;
        EnchantId = enchantId;
    }
}

[Serializable, NetSerializable]
public enum EnchantUiKey : byte
{
    Key
}