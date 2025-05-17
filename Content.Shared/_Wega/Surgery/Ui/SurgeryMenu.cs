using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable, NetSerializable]
public enum SurgeryUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryProcedureDto : BoundUserInterfaceMessage
{
    public List<SurgeryGroupDto> Groups;
    public NetEntity PatientId;

    public SurgeryProcedureDto(List<SurgeryGroupDto> groups, NetEntity patientId)
    {
        Groups = groups;
        PatientId = patientId;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryGroupDto
{
    public string GroupName;
    public string Description;
    public ProtoId<SurgeryNodePrototype> TargetNode;
    public List<SurgeryStepDto> Steps;

    public SurgeryGroupDto(string groupName, string description, ProtoId<SurgeryNodePrototype> targetNode, List<SurgeryStepDto> steps)
    {
        GroupName = groupName;
        Description = description;
        TargetNode = targetNode;
        Steps = steps;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryStepDto
{
    public string Name;
    public bool IsCompleted;
    public bool IsEnabled;
    public bool IsVisible;
    public string? RequiredTool;
    public string? RequiredCondition;

    public SurgeryStepDto(
        string name,
        bool isCompleted,
        bool isEnabled,
        bool isVisible,
        string? requiredTool,
        string? requiredCondition)
    {
        Name = name;
        IsCompleted = isCompleted;
        IsEnabled = isEnabled;
        IsVisible = isVisible;
        RequiredTool = requiredTool;
        RequiredCondition = requiredCondition;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryStartMessage : BoundUserInterfaceMessage
{
    public NetEntity User;
    public ProtoId<SurgeryNodePrototype> TargetNode;
    public int StepIndex;
    public bool IsParallel;

    public SurgeryStartMessage(NetEntity user, ProtoId<SurgeryNodePrototype> targetNode, int stepIndex, bool isParallel)
    {
        User = user;
        TargetNode = targetNode;
        StepIndex = stepIndex;
        IsParallel = isParallel;
    }
}
