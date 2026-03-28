using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

[Serializable, NetSerializable]
public enum ModularSuitUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ModularSuitBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool Active { get; }
    public float CoreCharge { get; }
    public float MaxCoreCharge { get; }
    public bool HasCore { get; }
    public bool InfinityCore { get; }
    public bool HasBattery { get; }
    public float BatteryCharge { get; }
    public float MaxBatteryCharge { get; }
    public float TotalPowerDraw { get; }
    public List<SuitModuleEntry> Modules { get; }
    public List<SuitPartEntry> Parts { get; }
    public string? WearerName { get; }

    public ModularSuitBoundUserInterfaceState(
        bool active,
        float coreCharge,
        float maxCoreCharge,
        bool hasCore,
        bool infinityCore,
        bool hasBattery,
        float batteryCharge,
        float maxBatteryCharge,
        float totalPowerDraw,
        List<SuitModuleEntry> modules,
        List<SuitPartEntry> parts,
        string? wearerName = null)
    {
        Active = active;
        CoreCharge = coreCharge;
        MaxCoreCharge = maxCoreCharge;
        HasCore = hasCore;
        InfinityCore = infinityCore;
        HasBattery = hasBattery;
        BatteryCharge = batteryCharge;
        MaxBatteryCharge = maxBatteryCharge;
        TotalPowerDraw = totalPowerDraw;
        Modules = modules;
        Parts = parts;
        WearerName = wearerName;
    }
}

[Serializable, NetSerializable]
public sealed class SuitModuleEntry
{
    public NetEntity ModuleUid { get; }
    public string Name { get; }
    public string ModuleId { get; }
    public bool IsActive { get; set; }
    public bool IsPermanent { get; set; }
    public float PowerUsage { get; }
    public float PowerInstanceUsage { get; }
    public bool CanBeDisabled { get; }
    public List<string> Tags { get; }

    public SuitModuleEntry(
        NetEntity moduleUid,
        string name,
        string moduleId,
        bool isActive,
        bool isPermanent,
        float powerUsage,
        float powerInstanceUsage,
        bool canBeDisabled,
        List<string> tags)
    {
        ModuleUid = moduleUid;
        Name = name;
        ModuleId = moduleId;
        IsActive = isActive;
        IsPermanent = isPermanent;
        PowerUsage = powerUsage;
        PowerInstanceUsage = powerInstanceUsage;
        CanBeDisabled = canBeDisabled;
        Tags = tags;
    }
}

[Serializable, NetSerializable]
public sealed class SuitPartEntry
{
    public NetEntity PartUid { get; }
    public string Name { get; }
    public SuitPartType PartType { get; }

    public SuitPartEntry(NetEntity partUid, string name, SuitPartType partType)
    {
        PartUid = partUid;
        Name = name;
        PartType = partType;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleSuitActiveMessage : BoundUserInterfaceMessage
{
    public bool Active { get; }

    public ToggleSuitActiveMessage(bool active)
    {
        Active = active;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleModuleMessage : BoundUserInterfaceMessage
{
    public NetEntity ModuleUid { get; }
    public bool Active { get; }

    public ToggleModuleMessage(NetEntity moduleUid, bool active)
    {
        ModuleUid = moduleUid;
        Active = active;
    }
}
