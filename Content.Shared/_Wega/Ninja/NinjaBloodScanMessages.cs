using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Ninja;

[Serializable, NetSerializable]
public enum NinjaBloodScanUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class NinjaBloodScanBuiState : BoundUserInterfaceState
{
    /// <summary>0 = Failed, 1 = Success, 2 = NotDone</summary>
    public readonly int[] ScanStates;
    public readonly string?[] DonorNames;
    public readonly NetEntity?[] VialEntities;
    public readonly bool BlockButtons;
    public readonly int ProgressBar;

    public NinjaBloodScanBuiState(
        int[] scanStates,
        string?[] donorNames,
        NetEntity?[] vialEntities,
        bool blockButtons,
        int progressBar)
    {
        ScanStates   = scanStates;
        DonorNames   = donorNames;
        VialEntities = vialEntities;
        BlockButtons = blockButtons;
        ProgressBar  = progressBar;
    }
}

/// <summary>Sent when a slot button is clicked: ejects vial if present, or inserts held item if empty.</summary>
[Serializable, NetSerializable]
public sealed class NinjaBloodScanSlotActionMessage : BoundUserInterfaceMessage
{
    public readonly int SlotIndex;
    public NinjaBloodScanSlotActionMessage(int slotIndex) { SlotIndex = slotIndex; }
}

[Serializable, NetSerializable]
public sealed class NinjaBloodScanScanMessage : BoundUserInterfaceMessage { }
