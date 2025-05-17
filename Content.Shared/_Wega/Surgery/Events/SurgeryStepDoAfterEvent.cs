using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;


[Serializable, NetSerializable]
public sealed partial class SurgeryStepDoAfterEvent : SimpleDoAfterEvent
{
    public ProtoId<SurgeryNodePrototype> TargetNode { get; }
    public bool IsParallel { get; }
    public int? StepIndex { get; }

    public SurgeryStepDoAfterEvent(ProtoId<SurgeryNodePrototype> targetNode, bool isParallel, int? stepIndex = null)
    {
        TargetNode = targetNode;
        IsParallel = isParallel;
        StepIndex = stepIndex;
    }
}