using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OperatedComponent : Component
{
    [DataField("raceGraph")]
    public ProtoId<SurgeryGraphPrototype>? GraphId;

    [DataField("currentNode")]
    public ProtoId<SurgeryNodePrototype> CurrentNode = "Default";

    [DataField("surgeon")]
    public EntityUid? Surgeon = default!;

    [DataField]
    public int CurrentStepIndex = 0;

    [DataField]
    public ProtoId<SurgeryNodePrototype>? CurrentTargetNode;

    [DataField]
    public HashSet<SurgeryStep> CompletedParallelSteps = new();

    [DataField]
    public Dictionary<ProtoId<SurgeryNodePrototype>, HashSet<SurgeryStep>> CompletedSteps = new();

    [ViewVariables]
    public float Sterility = 1f;

    [ViewVariables]
    public bool IsOperating;

    public void ResetOperationState(ProtoId<SurgeryNodePrototype> targetNode)
    {
        CurrentTargetNode = null;
        CurrentStepIndex = 0;
        IsOperating = false;
        Surgeon = null;

        if (targetNode == "Default")
        {
            CompletedSteps.Clear();
            CompletedParallelSteps.Clear();
        }
    }

    public void SetOperationState(ProtoId<SurgeryNodePrototype> targetNode, EntityUid surgeon)
    {
        CurrentTargetNode = targetNode;
        CurrentStepIndex = 0;
        IsOperating = true;
        Surgeon = surgeon;
    }
}
