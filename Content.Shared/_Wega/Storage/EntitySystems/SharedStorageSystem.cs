using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    [SubscribeLocalEvent]
    private void OnRecentlyOpenedGetState(EntityUid uid, RecentlyOpenedStoragesComponent component, ref ComponentGetState args)
    {
        var state = new RecentlyOpenedStoragesComponentState();

        foreach (var group in component.OpenedStorages)
        {
            var netGroup = new List<NetEntity>();
            foreach (var ent in group)
            {
                netGroup.Add(GetNetEntity(ent));
            }
            state.OpenedStorages.Add(netGroup);
        }

        args.State = state;
    }

    [SubscribeLocalEvent]
    private void OnRecentlyOpenedHandleState(EntityUid uid, RecentlyOpenedStoragesComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RecentlyOpenedStoragesComponentState state)
            return;

        component.OpenedStorages.Clear();

        foreach (var netGroup in state.OpenedStorages)
        {
            var group = new List<EntityUid>();
            foreach (var net in netGroup)
            {
                group.Add(EnsureEntity<RecentlyOpenedStoragesComponent>(net, uid));
            }
            component.OpenedStorages.Add(group);
        }
    }
}