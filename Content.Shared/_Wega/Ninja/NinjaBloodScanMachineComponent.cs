using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Ninja;

[RegisterComponent]
public sealed partial class NinjaBloodScanMachineComponent : Component
{
    public static readonly string[] SlotIds = { "blood-scan-vial-0", "blood-scan-vial-1", "blood-scan-vial-2" };

    public ContainerSlot[] VialSlots = new ContainerSlot[3];

    /// <summary>The ninja registered at this machine.</summary>
    public EntityUid? RegisteredNinja;

    /// <summary>Whether a scan sequence is currently running.</summary>
    public bool IsScanning;

    /// <summary>
    /// Current scan stage (0 = idle).
    /// 1=Activation → 2=Loading+slot0 → 3=slot1 → 4=slot2 → 5=end_eval →
    /// 6=WrongDeact → 7=WrongReset  (wrong path)
    /// 5=end_eval → 8=CorrectAnim → 9=CorrectDeact → 10=SuccessReset  (success path)
    /// Early-fail during stages 2-4 jumps directly to stage 6.
    /// </summary>
    public int ScanStage;

    public TimeSpan? NextStageTime;

    /// <summary>Scan progress 0-100.</summary>
    public int ProgressBar;

    /// <summary>Per-slot result: 0=Failed, 1=Success, 2=NotDone.</summary>
    public int[] ScanResults = { 2, 2, 2 };

    /// <summary>DNA strings already scanned (duplicate prevention).</summary>
    public HashSet<string> ScannedDnas = new();

    /// <summary>Cached donor name per slot, shown in BUI.</summary>
    public string?[] SlotDonorNames = { null, null, null };

    /// <summary>Cached DNA per slot, used during scan.</summary>
    public string?[] SlotDonorDnas = { null, null, null };

    /// <summary>Whether the last completed scan succeeded (used at stage 10).</summary>
    public bool LastScanSucceeded;
}

[Serializable, NetSerializable]
public enum BloodScanMachineVisuals : byte { State }

[Serializable, NetSerializable]
public enum BloodScanMachineState : byte
{
    Idle0,
    Idle1,
    Idle2,
    Idle3,
    Activation,
    Loading,
    Wrong,
    Correct,
    Deactivation,
}
