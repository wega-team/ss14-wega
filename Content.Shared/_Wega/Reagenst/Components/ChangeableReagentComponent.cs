using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.ChangeableReagent.Components;

[RegisterComponent, NetworkedComponent]
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
    public List<ReagentQuantity> Reagent;
	
    [DataField(required: true)]
    public LocId Name { get; set; }
}
