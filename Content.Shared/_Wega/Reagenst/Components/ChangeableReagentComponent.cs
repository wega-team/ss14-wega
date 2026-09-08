using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.ChangeableReagent.Components;

[RegisterComponent]
public sealed partial class ChangeableReagentComponent : Component
{
    [DataField(required: true)]
    public List<ChangeableReagents> Reagents = new();

    [DataField]
    public int CurrentReagent;
}

[DataDefinition]
public sealed partial class ChangeableReagents
{
    [DataField(required: true)]
    public List<ReagentQuantity> Reagent;

    [DataField(required: true)]
    public LocId Name { get; set; }
}
