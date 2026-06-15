using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Reagent.Ranged.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangeableReagentComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<ChangeableReagents> Reagents = new();

    [DataField]
    [AutoNetworkedField]
    public int CurrentAccount;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ChangeableReagents
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = "SpaceGlue";
}
