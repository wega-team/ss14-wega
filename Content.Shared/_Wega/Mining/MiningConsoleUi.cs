using Content.Shared.Mining.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Mining;

[Serializable, NetSerializable]
public enum MiningConsoleUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MiningConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float Credits;
    public float ResearchPoints;
    public MiningMode Mode;
    public bool GlobalActivation;
    public List<MiningServerData> Servers;

    public MiningConsoleBoundInterfaceState(
        float credits,
        float researchPoints,
        MiningMode mode,
        bool globalActivation,
        List<MiningServerData> servers)
    {
        Credits = credits;
        ResearchPoints = researchPoints;
        Mode = mode;
        GlobalActivation = globalActivation;
        Servers = servers;
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleModeMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleActivationMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleServerActivationMessage : BoundUserInterfaceMessage
{
    public NetEntity ServerUid;

    public MiningConsoleToggleServerActivationMessage(NetEntity serverUid)
    {
        ServerUid = serverUid;
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleChangeServerStageMessage : BoundUserInterfaceMessage
{
    public NetEntity ServerUid;
    public int Delta;

    public MiningConsoleChangeServerStageMessage(NetEntity serverUid, int delta)
    {
        ServerUid = serverUid;
        Delta = delta;
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleUpdateMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MiningConsoleWithdrawMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed record MiningServerData(
    NetEntity Uid,
    int Stage,
    float Temperature,
    bool IsBroken,
    bool IsActive
);
