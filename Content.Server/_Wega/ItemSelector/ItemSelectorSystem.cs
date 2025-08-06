using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.Selector.UI;
using Content.Shared.Item.Selector.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Item.Selector;

public sealed partial class ItemSelectorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemSelectorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ItemSelectorComponent, ItemSelectorSelectionMessage>(OnSelection);

    }

    private void OnUiOpened(EntityUid uid, ItemSelectorComponent comp, BoundUIOpenedEvent args)
    {
        if (!CheckComponents(args.Actor, comp.WhitelistComponents, comp.BlacklistComponents))
        {
            _ui.CloseUi(uid, ItemSelectorUiKey.Key);
            return;
        }

        UpdateUi(uid, comp.Items);
    }

    private bool CheckComponents(EntityUid entity, List<string> whitelist, List<string> blacklist)
    {
        if (whitelist.Count > 0 && !whitelist.All(component =>
            _componentFactory.TryGetRegistration(component, out var reg) && HasComp(entity, reg.Type)))
            return false;

        if (blacklist.Count > 0 && blacklist.Any(component =>
            _componentFactory.TryGetRegistration(component, out var reg) && HasComp(entity, reg.Type)))
            return false;

        return true;
    }

    private void UpdateUi(EntityUid uid, List<string> items)
    {
        if (!_ui.HasUi(uid, ItemSelectorUiKey.Key))
            return;

        _ui.ServerSendUiMessage(uid, ItemSelectorUiKey.Key,
            new ItemSelectorUserMessage(items));
    }

    private void OnSelection(EntityUid uid, ItemSelectorComponent comp, ItemSelectorSelectionMessage args)
    {
        var ent = Spawn(args.SelectedId, Transform(uid).Coordinates);
        _hands.TryForcePickupAnyHand(GetEntity(args.User), ent);

        QueueDel(uid);
    }
}
