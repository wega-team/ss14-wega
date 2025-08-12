using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OperatedComponent : Component
{
    /// <summary>
    /// The ID of the surgery graph that defines possible surgical procedures for this entity (for example, BaseSurgery).
    /// If null, the operation is impossible.
    /// </summary>
    [DataField("raceGraph"), AutoNetworkedField]
    public ProtoId<SurgeryGraphPrototype>? GraphId;

    /// <summary>
    /// The current node in the graph of operations.
    /// By default, "Default" is the initial state.
    /// </summary>
    [DataField("currentNode"), AutoNetworkedField]
    public ProtoId<SurgeryNodePrototype> CurrentNode = "Default";

    /// <summary>
    /// The target node to which the current operation leads.
    /// It is reset to null upon completion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SurgeryNodePrototype>? CurrentTargetNode;

    /// <summary>
    /// The ID of the entity (player) performing the operation.
    /// </summary>
    [DataField("surgeon")]
    public EntityUid? Surgeon = default!;

    /// <summary>
    /// The index of the current step in the sequence (starts from 0). Increases as it is executed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStepIndex = 0;

    /// <summary>
    /// Steps that can be performed in parallel (for example, simultaneous processing of several organs).
    /// </summary>
    [DataField]
    public HashSet<SurgeryStep> CompletedParallelSteps = new();

    /// <summary>
    /// A dictionary of completed steps for each node.э
    /// The key is the node ID, the value is a set of completed steps.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SurgeryNodePrototype>, HashSet<SurgeryStep>> CompletedSteps = new();

    /// <summary>
    /// Dictionary of internal damages, where the key is the ID of the damage prototype,
    /// and the value is a list of body parts that are affected.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<InternalDamagePrototype>, List<string>> InternalDamages = new();

    /// <summary>
    /// A modifier for the chance of losing a limb.
    /// </summary>
    [ViewVariables, DataField]
    public float LimbLossChance = 1f;

    /// <summary>
    /// The level of sterility. It may affect the chance of success.
    /// </summary>
    [ViewVariables]
    public float Sterility = 1f;

    /// <summary>
    /// Responsible for updating pain in internal injuries and updating sterility.
    /// </summary>
    [ViewVariables]
    public float NextUpdateTick = default!;

    /// <summary>
    /// A flag indicating that the operation is in progress.
    /// Blocks other interactions.
    /// </summary>
    [ViewVariables]
    public bool IsOperating;

    /// <summary>
    /// A flag indicating that the operation will be performed on a body part, and not on a humanoid (for example, the head).
    /// </summary>
    [DataField, ViewVariables]
    public bool OperatedPart = false;

    /// <summary>
    /// Resets the operation status.
    /// </summary>
    /// <param name="targetNode">Which node is being reset to, if it is Default, resets the completed steps, zeroing out the logic</param>
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

    /// <summary>
    /// Sets the new state of the operation.
    /// </summary>
    /// <param name="targetNode">Purpose of the operation</param>
    /// <param name="surgeon">The correct surgeon</param>
    public void SetOperationState(ProtoId<SurgeryNodePrototype> targetNode, EntityUid surgeon)
    {
        CurrentTargetNode = targetNode;
        CurrentStepIndex = 0;
        IsOperating = true;
        Surgeon = surgeon;
    }
}
