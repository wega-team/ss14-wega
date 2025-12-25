using Content.Shared.Voucher.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Voucher;

[Serializable, NetSerializable]
public enum VoucherUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VoucherKitSelectionState : BoundUserInterfaceState
{
    public readonly List<ProtoId<VoucherKitPrototype>> AvailableKits;

    public VoucherKitSelectionState(List<ProtoId<VoucherKitPrototype>> availableKits)
    {
        AvailableKits = availableKits;
    }
}

[Serializable, NetSerializable]
public sealed class VoucherKitSelectedMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity User;
    public readonly ProtoId<VoucherKitPrototype> KitId;

    public VoucherKitSelectedMessage(NetEntity user, ProtoId<VoucherKitPrototype> kitId)
    {
        User = user;
        KitId = kitId;
    }
}
