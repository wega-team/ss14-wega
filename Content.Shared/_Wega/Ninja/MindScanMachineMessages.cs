using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Ninja;

[Serializable, NetSerializable]
public enum MindScanMachineUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MindScanMachineBuiState : BoundUserInterfaceState
{
    public readonly NetEntity? Occupant;
    public readonly float TotalDamage;
    public readonly bool IsAlive;
    public readonly bool IsScanning;
    public readonly bool ScanComplete;
    public readonly List<string> ScannedNames;

    public MindScanMachineBuiState(
        NetEntity? occupant,
        float totalDamage,
        bool isAlive,
        bool isScanning,
        bool scanComplete,
        List<string> scannedNames)
    {
        Occupant = occupant;
        TotalDamage = totalDamage;
        IsAlive = isAlive;
        IsScanning = isScanning;
        ScanComplete = scanComplete;
        ScannedNames = scannedNames;
    }
}

[Serializable, NetSerializable]
public sealed class MindScanScanMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class MindScanEjectMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class MindScanTeleportMessage : BoundUserInterfaceMessage { }
