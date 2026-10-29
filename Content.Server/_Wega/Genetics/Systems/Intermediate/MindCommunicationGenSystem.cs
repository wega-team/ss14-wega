using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Prayer;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Genetics;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.MindCommunication;

public sealed partial class MindCommunicationGenSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private ServerPrayerSystem _prayerSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<MindCommunicationGenComponent> ent, ref ComponentInit args)
        => ent.Comp.ActionEntity = _action.AddAction(ent, ent.Comp.Action);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<MindCommunicationGenComponent> ent, ref ComponentShutdown args)
        => _action.RemoveAction(ent.Comp.ActionEntity);

    [SubscribeLocalEvent]
    private void OnMindCommunication(Entity<MindCommunicationGenComponent> ent, ref MindCommunicationActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        RaiseNetworkEvent(new MindCommunicationMenuOpenedEvent(GetNetEntity(ent)));
    }

    [SubscribeNetworkEvent]
    private void OnTargetSelected(MindCommunicationTargetSelectedEvent args, EntitySessionEventArgs session)
    {
        if (session.SenderSession.AttachedEntity is not { } sender)
            return;

        if (!HasComp<MindCommunicationGenComponent>(sender))
            return;

        var target = GetEntity(args.Target);
        if (!Exists(target))
            return;

        if (!TryComp<ActorComponent>(sender, out var senderActor) ||
            !TryComp<ActorComponent>(target, out var targetActor))
            return;

        if (HasComp<PsyResistGenComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("mind-communication-blocked", ("name", Name(target))), sender, sender);
            return;
        }

        var senderUid = sender;
        var targetUid = target;

        _quickDialog.OpenDialog(senderActor.PlayerSession,
            Loc.GetString("mind-communication-dialog-title"), "",
            (string message) =>
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;

                if (!Exists(senderUid) || !Exists(targetUid) || !HasComp<MindCommunicationGenComponent>(senderUid))
                    return;

                var escapedMessage = FormattedMessage.EscapeText(message);
                var popupMessage = Loc.GetString("mind-communication-message", ("message", escapedMessage));
                _prayerSystem.SendSubtleMessage(targetActor.PlayerSession, targetActor.PlayerSession, string.Empty, popupMessage);

                _admin.Add(LogType.Chat, LogImpact.Low,
                    $"{ToPrettyString(senderUid):user} sent mind message to {ToPrettyString(targetUid):target}: {message}");
            });
    }
}
