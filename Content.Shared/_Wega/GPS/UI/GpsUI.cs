using Robust.Shared.Serialization;

namespace Content.Shared.GPS;

[Serializable, NetSerializable]
public enum GpsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GpsUpdateState : BoundUserInterfaceState
{
    public NetEntity? MapUid { get; }
    public string CurrentGpsName { get; }
    public string CurrentGpsDesc { get; }
    public bool BroadcastStatus { get; }
    public (int X, int Y) CurrentCoordinates { get; }
    public List<GpsDeviceInfo> OtherGpsDevices { get; }
    public List<NavBeaconInfo> NavBeacons { get; }
    public List<LavaTileInfo> LavaTiles { get; }

    public GpsUpdateState(
        NetEntity? mapUid,
        string currentGpsName,
        string currentGpsDesc,
        bool broadcastStatus,
        (int X, int Y) currentCoordinates,
        List<GpsDeviceInfo> otherGpsDevices,
        List<NavBeaconInfo> navBeacons,
        List<LavaTileInfo> lavaTiles)
    {
        MapUid = mapUid;
        CurrentGpsName = currentGpsName;
        CurrentGpsDesc = currentGpsDesc;
        BroadcastStatus = broadcastStatus;
        CurrentCoordinates = currentCoordinates;
        OtherGpsDevices = otherGpsDevices;
        NavBeacons = navBeacons;
        LavaTiles = lavaTiles;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateGpsNameMessage : BoundUserInterfaceMessage
{
    public string NewName { get; }

    public UpdateGpsNameMessage(string newName)
    {
        NewName = newName;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateGpsDescriptionMessage : BoundUserInterfaceMessage
{
    public string NewDescription { get; }

    public UpdateGpsDescriptionMessage(string newDescription)
    {
        NewDescription = newDescription;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleGpsBroadcastMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public ToggleGpsBroadcastMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class GpsDeviceInfo
{
    public NetEntity EntityUid { get; }
    public string Name { get; }
    public (int X, int Y) Coordinates { get; }
    public float Distance { get; }
    public string Description { get; }

    public GpsDeviceInfo(
        NetEntity entityUid,
        string name,
        (int X, int Y) coordinates,
        float distance,
        string description)
    {
        EntityUid = entityUid;
        Name = name;
        Coordinates = coordinates;
        Distance = distance;
        Description = description;
    }
}

[Serializable, NetSerializable]
public sealed class NavBeaconInfo
{
    public string Name { get; }
    public string Desc { get; }
    public (int X, int Y) Coordinates { get; }
    public float Distance { get; }
    public Color Color { get; }
    public bool Enabled { get; }

    public NavBeaconInfo(
        string name,
        string desc,
        (int X, int Y) coordinates,
        float distance,
        Color color,
        bool enabled)
    {
        Name = name;
        Desc = desc;
        Coordinates = coordinates;
        Distance = distance;
        Color = color;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class LavaTileInfo
{
    public (int X, int Y) Coordinates { get; }
    public Color Color { get; }
    public float Distance { get; }

    public LavaTileInfo(
        (int X, int Y) coordinates,
        Color color,
        float distance)
    {
        Coordinates = coordinates;
        Color = color;
        Distance = distance;
    }
}
