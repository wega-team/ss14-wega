using Content.Shared.Chemistry.Reagent;
using Content.Shared.Genetics.UI;
using Robust.Shared.Serialization;

namespace Content.Shared.Genetics;

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleEjectEvent : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleEjectRejuveEvent : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleReagentButtonEvent : BoundUserInterfaceMessage
{
    public DnaModifierReagentAmount Amount { get; }
    public ReagentId ReagentId { get; }

    public DnaModifierConsoleReagentButtonEvent(DnaModifierReagentAmount amount, ReagentId reagentId)
    {
        Amount = amount;
        ReagentId = reagentId;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleSaveServerEvent : BoundUserInterfaceMessage
{
    public int CurrentSection { get; }
    public int CurrentType { get; }

    public DnaModifierConsoleSaveServerEvent(int currentSection, int currentType)
    {
        CurrentSection = currentSection;
        CurrentType = currentType;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleInjectorEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleInjectorEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierInjectBlockEvent : BoundUserInterfaceMessage
{
    public int Index { get; }
    public int CurrentBlock { get; }

    public DnaModifierInjectBlockEvent(int index, int currentBlock)
    {
        Index = index;
        CurrentBlock = currentBlock;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleSubjectInjectEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleSubjectInjectEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleClearBufferEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleClearBufferEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleRenameBufferEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleRenameBufferEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleExportOnDiskEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleExportOnDiskEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleExportFromDiskEvent : BoundUserInterfaceMessage
{
    public int Index { get; }

    public DnaModifierConsoleExportFromDiskEvent(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleClearDiskEvent : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleReleverationEvent : BoundUserInterfaceMessage
{
    public int CurrentTab { get; }
    public string CurrentBlock { get; }
    public int CurrentValue { get; }
    public float Intensity { get; }

    public DnaModifierConsoleReleverationEvent(int currentTab, string currentBlock, int currentValue, float intensity)
    {
        CurrentTab = currentTab;
        CurrentBlock = currentBlock;
        CurrentValue = currentValue;
        Intensity = intensity;
    }
}

[Serializable, NetSerializable]
public sealed partial class DnaModifierConsoleReleverationsEvent : BoundUserInterfaceMessage
{
    public int CurrentTab { get; }
    public float Intensity { get; }
    public float Duration { get; }

    public DnaModifierConsoleReleverationsEvent(int currentTab, float intensity, float duration)
    {
        CurrentTab = currentTab;
        Intensity = intensity;
        Duration = duration;
    }
}

public sealed partial class CureDnaDiseaseAttemptEvent : EntityEventArgs
{
    public float CureChance { get; }
    public CureDnaDiseaseAttemptEvent(float cureChance) { CureChance = cureChance; }
}

public sealed partial class MutateDnaAttemptEvent : EntityEventArgs
{
    public MutateDnaAttemptEvent() { }
}
