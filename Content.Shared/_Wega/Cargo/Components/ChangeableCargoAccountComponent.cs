using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Radio;
using Content.Shared.Cargo.Prototypes;

namespace Content.Shared.Cargo.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangeableCargoAccountComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<ChangeableCargoAccount> Accounts = new();

    [DataField]
    [AutoNetworkedField]
    public int CurrentAccount;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ChangeableCargoAccount
{
    [DataField(required: true)]
    public ProtoId<CargoAccountPrototype> Account = "Cargo";

    [DataField]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Supply";
}
