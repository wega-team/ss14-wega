using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Ninja;

[RegisterComponent]
public sealed partial class MindScanMachineComponent : Component
{
    public ContainerSlot BodyContainer = default!;
    public bool Scanning;
    public bool ScanComplete;
    [DataField] public TimeSpan SleepDuration = TimeSpan.FromSeconds(30);
    public int ScanStage;
    public TimeSpan? NextStageTime;
    public EntityUid? ScanInitiator;
}

[Serializable, NetSerializable]
public enum MindScanMachineVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum MindScanMachineStatus : byte
{
    Open,
    Occupied,
}
