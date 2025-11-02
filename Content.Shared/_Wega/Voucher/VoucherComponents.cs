using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Voucher.Components;

[RegisterComponent]
public sealed partial class VoucherComponent : Component
{
    [DataField("typeVoucher", required: true)]
    public string TypeVoucher;

    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
}

[RegisterComponent]
public sealed partial class VoucherCardComponent : Component
{
    [DataField("typeVoucher", required: true)]
    public string TypeVoucher;

    [DataField("kits", required: true)]
    public List<ProtoId<VoucherKitPrototype>> Kits = new();

    public ProtoId<VoucherKitPrototype>? CurrentKit = default!;
}

[Prototype("voucherKit")]
public sealed partial class VoucherKitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("items", required: true)]
    public List<EntProtoId> Items = new();

    [DataField("icon")]
    public SpriteSpecifier? Icon;
}
