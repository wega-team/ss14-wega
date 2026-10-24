using System.Linq;
using Content.Shared.Item.Selector.UI;
using Content.Shared.Item.Selector.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Item.Selector;

public sealed partial class ObjectSelectorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObjectSelectorComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ObjectSelectorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ObjectSelectorComponent, ObjectSelectorSelectionMessage>(OnSelection);
    }

    private void OnInteract(EntityUid uid, ObjectSelectorComponent comp, InteractHandEvent args)
    {
        if (!_ui.HasUi(uid, ObjectSelectorUiKey.Key) || comp.DisabledInteract)
            return;

        _ui.OpenUi(uid, ObjectSelectorUiKey.Key, args.User);
    }

    private void OnUiOpened(EntityUid uid, ObjectSelectorComponent comp, BoundUIOpenedEvent args)
    {
        if (!CheckComponents(args.Actor, comp.WhitelistComponents, comp.BlacklistComponents))
        {
            _ui.CloseUi(uid, ObjectSelectorUiKey.Key);
            return;
        }

        UpdateUi(uid, comp.Objects);
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

    private void UpdateUi(EntityUid uid, List<EntProtoId> objects)
    {
        if (!_ui.HasUi(uid, ObjectSelectorUiKey.Key))
            return;

        _ui.ServerSendUiMessage(uid, ObjectSelectorUiKey.Key,
            new ObjectSelectorUserMessage(objects));
    }

    private void OnSelection(EntityUid uid, ObjectSelectorComponent comp, ObjectSelectorSelectionMessage args)
    {
        if (!comp.Objects.Contains(args.SelectedId))
            return;

        var user = args.Actor;
        if (!_interaction.InRangeUnobstructed(user, uid))
            return;

        var ent = Spawn(args.SelectedId, Transform(uid).Coordinates);

        _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):user} selects {ToPrettyString(ent):entity} instead of {ToPrettyString(uid):entity}");

        QueueDel(uid);
    }
}
