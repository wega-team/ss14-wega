using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable, NetSerializable]
public enum SurgeryUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class SurgeryProcedureDtoState : BoundUserInterfaceState
{
    public List<SurgeryGroupDto> Groups;
    public SurgerySterilityInfo SterilityInfo;

    public SurgeryProcedureDtoState(List<SurgeryGroupDto> groups, SurgerySterilityInfo sterilityInfo)
    {
        Groups = groups;
        SterilityInfo = sterilityInfo;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryGroupDto
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
public sealed partial class SurgeryStepDto
{
    public string Name;
    public bool IsCompleted;
    public bool IsEnabled;
    public bool IsVisible;
    public string? RequiredTool;
    public string? RequiredCondition;
    public string? EntityPreview;

    public SurgeryStepDto(
        string name,
        bool isCompleted,
        bool isEnabled,
        bool isVisible,
        string? requiredTool,
        string? requiredCondition,
        string? entityPreview)
    {
        Name = name;
        IsCompleted = isCompleted;
        IsEnabled = isEnabled;
        IsVisible = isVisible;
        RequiredTool = requiredTool;
        RequiredCondition = requiredCondition;
        EntityPreview = entityPreview;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgerySterilityInfo
{
    public float Sterility;
    public List<string> NegativeFactors;
    public List<string> PositiveFactors;

    public SurgerySterilityInfo(float sterility, List<string> negativeFactors, List<string> positiveFactors)
    {
        Sterility = sterility;
        NegativeFactors = negativeFactors;
        PositiveFactors = positiveFactors;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgerySterilityUpdateMessage : BoundUserInterfaceMessage
{
    public SurgerySterilityInfo SterilityInfo;

    public SurgerySterilityUpdateMessage(SurgerySterilityInfo sterilityInfo)
    {
        SterilityInfo = sterilityInfo;
    }
}

[Serializable, NetSerializable]
public sealed partial class SurgeryStartMessage : BoundUserInterfaceMessage
{
    public ProtoId<SurgeryNodePrototype> TargetNode;
    public int StepIndex;
    public bool IsParallel;

    public SurgeryStartMessage(ProtoId<SurgeryNodePrototype> targetNode, int stepIndex, bool isParallel)
    {
        TargetNode = targetNode;
        StepIndex = stepIndex;
        IsParallel = isParallel;
    }
}
