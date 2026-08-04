using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Ninja;

[RegisterComponent]
public sealed partial class NinjaTeleConsoleComponent : Component
{
}

[Serializable, NetSerializable]
public enum NinjaTeleConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NinjaTeleConsoleBuiState : BoundUserInterfaceState
{
    public readonly bool PlayerNearbyAtDestination;
    public readonly string? NearbyPlayerName;

    public NinjaTeleConsoleBuiState(bool playerNearby, string? nearbyPlayerName)
    {
        PlayerNearbyAtDestination = playerNearby;
        NearbyPlayerName = nearbyPlayerName;
    }
}

[Serializable, NetSerializable]
public sealed class NinjaTeleConsoleConfirmMessage : BoundUserInterfaceMessage
{
}
