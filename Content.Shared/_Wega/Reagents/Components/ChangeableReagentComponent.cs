using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ChangeableReagent.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ChangeableReagentSystem))]
[AutoGenerateComponentState]
public sealed partial class ChangeableReagentComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<ChangeableReagents> Reagents = new();

    [DataField]
    [AutoNetworkedField]
    public int CurrentReagent;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ChangeableReagents
{
    [DataField(required: true)]
    public List<ReagentQuantity> Reagent = default!;
}
