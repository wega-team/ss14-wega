using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components;

/// <summary>
///     Attached to an actor to track which storages it has opened, and in what order
///     so the latest can be auto-closed when a new window is opened past the limit
/// </summary>

[RegisterComponent, Access(typeof(SharedStorageSystem)), NetworkedComponent]
public sealed partial class RecentlyOpenedStoragesComponent : Component
{
    [ViewVariables]
    public List<List<EntityUid>> OpenedStorages = new();
}

[Serializable, NetSerializable]
public sealed class RecentlyOpenedStoragesComponentState : ComponentState
{
    public List<List<NetEntity>> OpenedStorages = new();
}