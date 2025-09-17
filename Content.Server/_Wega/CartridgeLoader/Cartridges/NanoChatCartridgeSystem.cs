using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Emp;
using Content.Server.StationEvents.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Emp;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.PDA;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : SharedNanoChatCartridgeSystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<string, EntityUid> _activeChats = new();
    private readonly Dictionary<string, ChatGroupData> _groups = new();
    private const int MessageRange = 2000;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<PdaComponent, OwnerNameChangedEvent>(OnOwnerNameChanged);

        SubscribeLocalEvent<NanoChatCartridgeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);

        SubscribeLocalEvent<NanoChatCartridgeComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<NanoChatCartridgeComponent, EmpDisabledRemoved>(OnEmpFinished);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args) { _activeChats.Clear(); _groups.Clear(); }

    private void OnOwnerNameChanged(Entity<PdaComponent> ent, ref OwnerNameChangedEvent args)
    {
        var container = _container.GetContainer(ent, "program-container");
        if (container.ContainedEntities.Count == 0)
            return;

        foreach (var cartridgeUid in container.ContainedEntities)
        {
            if (!TryComp<NanoChatCartridgeComponent>(cartridgeUid, out var nanoChat) || string.IsNullOrEmpty(ent.Comp.OwnerName))
                continue;

            nanoChat.OwnerName = ent.Comp.OwnerName;
            break;
        }
    }

    private void OnMapInit(Entity<NanoChatCartridgeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ChatId = GenerateUniqueChatId();
        ent.Comp.OwnerName = Loc.GetString("generic-unknown-title");

        _activeChats[ent.Comp.ChatId] = ent.Owner;
    }

    private string GenerateUniqueChatId()
    {
        string id;
        do
        {
            id = "#" + _random.Next(10000).ToString("D4");
        } while (_activeChats.ContainsKey(id));

        return id;
    }

    private string GenerateUniqueGroupId()
    {
        string id;
        do
        {
            id = "G" + _random.Next(10000).ToString("D4");
        } while (_groups.ContainsKey(id));

        return id;
    }

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnCartridgeRemoved(Entity<NanoChatCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        _activeChats.Remove(ent.Comp.ChatId);
        foreach (var group in _groups.Values.Where(g => g.Members.Contains(ent.Comp.ChatId)).ToList())
            group.Members.Remove(ent.Comp.ChatId);
    }

    private void OnEmpPulse(Entity<NanoChatCartridgeComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        DistortAllMessages(ent.Comp);

        UpdateUiState(ent);
    }

    private void OnEmpFinished(Entity<NanoChatCartridgeComponent> ent, ref EmpDisabledRemoved args)
    {
        UpdateUiState(ent);
    }

    private void OnUiMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not NanoChatUiMessageEvent message)
            return;

        switch (message.Payload)
        {
            case NanoChatAddContact addContact:
                AddContact(ent, addContact.ContactId, addContact.ContactName);
                break;
            case NanoChatEraseContact addContact:
                EraseContact(ent, addContact.ContactId);
                break;
            case NanoChatMuted:
                SwitchMuted(ent);
                break;
            case NanoChatSendMessage sendMessage:
                SendMessage(ent, sendMessage.RecipientId, sendMessage.Message);
                break;
            case NanoChatSetActiveChat setActiveChat:
                SetActiveChat(ent, setActiveChat.ContactId);
                break;
            case NanoChatCreateGroup createGroup:
                CreateGroup(ent, createGroup.GroupName);
                break;
            case NanoChatJoinGroup joinGroup:
                JoinGroup(ent, joinGroup.GroupId);
                break;
            case NanoChatLeaveGroup leaveGroup:
                LeaveGroup(ent, leaveGroup.GroupId);
                break;
        }

        UpdateUiState(ent);
    }

    private void AddContact(Entity<NanoChatCartridgeComponent> ent, string contactId, string contactName)
    {
        if (!ent.Comp.Contacts.ContainsKey(contactId))
        {
            ent.Comp.Contacts[contactId] = new ChatContact(contactId, contactName, false);
        }
    }

    private void EraseContact(Entity<NanoChatCartridgeComponent> ent, string contactId)
    {
        if (ent.Comp.Contacts.ContainsKey(contactId))
            ent.Comp.Contacts.Remove(contactId);

        ent.Comp.ActiveChat = null;
        UpdateUiState(ent);
    }

    private void SwitchMuted(Entity<NanoChatCartridgeComponent> ent)
    {
        ent.Comp.MutedSound = !ent.Comp.MutedSound;
        UpdateUiState(ent);
    }

    private void SetActiveChat(Entity<NanoChatCartridgeComponent> ent, string chatId)
    {
        ent.Comp.ActiveChat = chatId;
        if (ent.Comp.Contacts.TryGetValue(chatId, out var contact))
        {
            ent.Comp.Contacts[chatId] = new ChatContact(chatId, contact.ContactName, false);
        }

        if (ent.Comp.Groups.TryGetValue(chatId, out var group))
        {
            ent.Comp.Groups[chatId] = new ChatGroup(chatId, group.GroupName, false, group.MemberCount);
        }

        UpdateUiState(ent);
    }

    private void CreateGroup(Entity<NanoChatCartridgeComponent> ent, string groupName)
    {
        var groupId = GenerateUniqueGroupId();
        var groupData = new ChatGroupData
        {
            GroupId = groupId,
            GroupName = groupName
        };

        _groups[groupId] = groupData;

        var systemMessage = new ChatMessage(
            "system", "System",
            Loc.GetString("nano-chat-create-group-message", ("name", ent.Comp.OwnerName), ("groupName", groupName)),
            _timing.CurTime,
            false,
            true
        );

        groupData.Messages.Add(systemMessage);

        JoinGroup(ent, groupId);

        _admin.Add(LogType.Action, LogImpact.Low,
            $"Group created: '{groupName}' (ID: {groupId}) by {ent.Comp.ChatId}");
    }

    private void JoinGroup(Entity<NanoChatCartridgeComponent> ent, string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.Members.Add(ent.Comp.ChatId);

            ent.Comp.Groups[groupId] = new ChatGroup(
                groupId,
                group.GroupName,
                false,
                group.Members.Count
            );

            NotifyGroupMembers(groupId, Loc.GetString("nano-chat-join-message", ("name", ent.Comp.OwnerName)));

            UpdateAllGroupMembersUi(groupId);
        }
    }

    private void LeaveGroup(Entity<NanoChatCartridgeComponent> ent, string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.Members.Remove(ent.Comp.ChatId);
            ent.Comp.Groups.Remove(groupId);

            NotifyGroupMembers(groupId, Loc.GetString("nano-chat-leave-message", ("name", ent.Comp.OwnerName)));

            if (ent.Comp.ActiveChat == groupId)
            {
                ent.Comp.ActiveChat = null;
            }

            UpdateAllGroupMembersUi(groupId);
        }
    }

    private void SendMessage(Entity<NanoChatCartridgeComponent> sender, string recipientId, string message)
    {
        if (_timing.CurTime < sender.Comp.NextMessageAllowedAfter)
            return;

        message = message.Length <= 256 ? message : message[..256];
        sender.Comp.NextMessageAllowedAfter = _timing.CurTime + sender.Comp.MessageDelay;

        var originalMessage = message;
        if (HasCommunicationDisturbance(sender, out var isPower, out var isSolar))
        {
            if (isPower)
            {
                AddUndeliveredMessage(sender, recipientId, originalMessage);
                UpdateUiState(sender);
                return;
            }
            else if (isSolar)
            {
                if (_random.Prob(0.4f))
                {
                    AddUndeliveredMessage(sender, recipientId, originalMessage);
                    UpdateUiState(sender);
                    return;
                }

                message = DistortMessage(message);
            }
        }

        if (HasComp<EmpDisabledComponent>(sender))
        {
            AddUndeliveredMessage(sender, recipientId, message);
            UpdateUiState(sender);
            return;
        }

        if (recipientId.StartsWith("G") && _groups.TryGetValue(recipientId, out var group))
        {
            SendGroupMessage(sender, group, message, originalMessage);
            return;
        }

        if (!_activeChats.TryGetValue(recipientId, out var recipientEntity))
        {
            AddUndeliveredMessage(sender, recipientId, originalMessage);
            UpdateUiState(sender);
            return;
        }

        if (!TryComp<NanoChatCartridgeComponent>(recipientEntity, out var recipientComp))
        {
            AddUndeliveredMessage(sender, recipientId, originalMessage);
            UpdateUiState(sender);
            return;
        }

        if (!IsWithinRange(sender.Owner, recipientEntity))
        {
            AddUndeliveredMessage(sender, recipientId, originalMessage);
            UpdateUiState(sender);
            return;
        }

        var timestamp = _timing.CurTime;
        var senderMessage = new ChatMessage(sender.Comp.ChatId, sender.Comp.OwnerName,
            originalMessage, timestamp, true, true);
        var recipientMessage = new ChatMessage(sender.Comp.ChatId, sender.Comp.OwnerName,
            message, timestamp, false, true);

        // Add to sender's history
        if (!sender.Comp.Messages.ContainsKey(recipientId))
            sender.Comp.Messages[recipientId] = new List<ChatMessage>();
        sender.Comp.Messages[recipientId].Add(senderMessage);

        // Add to recipient's history and mark as unread
        if (!recipientComp.Messages.ContainsKey(sender.Comp.ChatId))
            recipientComp.Messages[sender.Comp.ChatId] = new List<ChatMessage>();
        recipientComp.Messages[sender.Comp.ChatId].Add(recipientMessage);

        // Mark as unread in recipient's contacts
        if (recipientComp.Contacts.TryGetValue(sender.Comp.ChatId, out var contact))
        {
            recipientComp.Contacts[sender.Comp.ChatId] = new ChatContact(
                contact.ContactId,
                contact.ContactName,
                true);
        }
        else
        {
            recipientComp.Contacts[sender.Comp.ChatId] = new ChatContact(
                sender.Comp.ChatId,
                sender.Comp.OwnerName,
                true);
        }

        _admin.Add(LogType.Action, LogImpact.Low, $"Nano message: '{originalMessage}' by {recipientId} -> {sender.Comp.ChatId} delivered.");

        if (TryComp<CartridgeComponent>(recipientEntity, out var cartridge)
            && cartridge.LoaderUid.HasValue && !recipientComp.MutedSound)
            _audio.PlayPvs(recipientComp.Sound, recipientEntity);

        UpdateUiState((recipientEntity, recipientComp));
        UpdateUiState(sender);
    }

    private void SendGroupMessage(Entity<NanoChatCartridgeComponent> sender, ChatGroupData group, string message, string originalMessage)
    {
        var timestamp = _timing.CurTime;
        var groupMessage = new ChatMessage(sender.Comp.ChatId, sender.Comp.OwnerName,
            message, timestamp, true, true);

        group.Messages.Add(groupMessage);

        if (!sender.Comp.Messages.ContainsKey(group.GroupId))
            sender.Comp.Messages[group.GroupId] = new List<ChatMessage>();
        sender.Comp.Messages[group.GroupId].Add(groupMessage);

        foreach (var memberId in group.Members)
        {
            if (memberId == sender.Comp.ChatId)
                continue;

            if (!_activeChats.TryGetValue(memberId, out var memberEntity))
                continue;

            if (!TryComp<NanoChatCartridgeComponent>(memberEntity, out var memberComp))
                continue;

            if (!IsWithinRange(sender.Owner, memberEntity))
                continue;

            if (!memberComp.Messages.ContainsKey(group.GroupId))
                memberComp.Messages[group.GroupId] = new List<ChatMessage>();

            var memberMessage = new ChatMessage(sender.Comp.ChatId, sender.Comp.OwnerName,
                message, timestamp, false, true);
            memberComp.Messages[group.GroupId].Add(memberMessage);

            if (memberComp.Groups.TryGetValue(group.GroupId, out var memberGroup))
            {
                memberComp.Groups[group.GroupId] = new ChatGroup(
                    memberGroup.GroupId,
                    memberGroup.GroupName,
                    true,
                    group.Members.Count
                );
            }

            if (TryComp<CartridgeComponent>(memberEntity, out var cartridge)
                && cartridge.LoaderUid.HasValue && !memberComp.MutedSound)
                _audio.PlayPvs(memberComp.Sound, memberEntity);

            UpdateUiState((memberEntity, memberComp));
        }

        _admin.Add(LogType.Action, LogImpact.Low,
            $"Group message: '{originalMessage}' by {sender.Comp.ChatId} in group {group.GroupId}.");

        UpdateUiState(sender);
    }

    private void UpdateUiState(Entity<NanoChatCartridgeComponent> ent)
    {
        List<ChatMessage>? activeMessages = null;
        if (ent.Comp.ActiveChat != null)
        {
            if (ent.Comp.ActiveChat.StartsWith("G") && _groups.TryGetValue(ent.Comp.ActiveChat, out var group))
            {
                activeMessages = group.Messages;
            }
            else if (ent.Comp.Messages.TryGetValue(ent.Comp.ActiveChat, out var messages))
            {
                activeMessages = messages;
            }
        }

        if (!TryComp<CartridgeComponent>(ent, out var cartridge) || !cartridge.LoaderUid.HasValue)
            return;

        var state = new NanoChatUiState(ent.Comp.ChatId, ent.Comp.ActiveChat, ent.Comp.MutedSound, ent.Comp.Contacts, ent.Comp.Groups, activeMessages);
        _cartridgeLoader.UpdateCartridgeUiState(cartridge.LoaderUid.Value, state);
    }

    private void UpdateAllGroupMembersUi(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            foreach (var memberId in group.Members)
            {
                if (_activeChats.TryGetValue(memberId, out var memberEntity) &&
                    TryComp<NanoChatCartridgeComponent>(memberEntity, out var memberComp))
                {

                    memberComp.Groups[groupId] = new ChatGroup(
                        groupId,
                        group.GroupName,
                        memberComp.Groups.TryGetValue(groupId, out var currentGroup) ? currentGroup.HasUnread : false,
                        group.Members.Count
                    );

                    UpdateUiState((memberEntity, memberComp));
                }
            }
        }
    }

    private bool IsWithinRange(EntityUid sender, EntityUid recipient)
    {
        if (!Transform(sender).Coordinates.IsValid(EntityManager) ||
            !Transform(recipient).Coordinates.IsValid(EntityManager))
            return false;

        var senderCoords = _transform.GetMapCoordinates(sender);
        var recipientCoords = _transform.GetMapCoordinates(recipient);
        if (senderCoords.MapId != recipientCoords.MapId)
            return false;

        return senderCoords.InRange(recipientCoords, MessageRange);
    }

    private void NotifyGroupMembers(string groupId, string message)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            var timestamp = _timing.CurTime;
            var groupMessage = new ChatMessage("system", "System", message, timestamp, false, true);
            group.Messages.Add(groupMessage);

            foreach (var memberId in group.Members)
            {
                if (_activeChats.TryGetValue(memberId, out var memberEntity) &&
                    TryComp<NanoChatCartridgeComponent>(memberEntity, out var memberComp) &&
                    TryComp<CartridgeComponent>(memberEntity, out var cartridge) &&
                    cartridge.LoaderUid.HasValue && !memberComp.MutedSound)
                {
                    _audio.PlayPvs(memberComp.Sound, memberEntity);
                }
            }
        }
    }

    private void AddUndeliveredMessage(Entity<NanoChatCartridgeComponent> sender, string recipientId, string message)
    {
        var timestamp = _timing.CurTime;
        var undeliveredMessage = new ChatMessage(sender.Comp.ChatId, sender.Comp.OwnerName,
            message, timestamp, true, false);

        if (!sender.Comp.Messages.ContainsKey(recipientId))
            sender.Comp.Messages[recipientId] = new List<ChatMessage>();

        sender.Comp.Messages[recipientId].Add(undeliveredMessage);
    }

    private bool HasCommunicationDisturbance(Entity<NanoChatCartridgeComponent> sender, out bool isPower, out bool isSolar)
    {
        isPower = false;
        isSolar = false;

        var powerGridQuery = EntityQueryEnumerator<PowerGridCheckRuleComponent, GameRuleComponent>();
        while (powerGridQuery.MoveNext(out var ev, out _, out var gameRuleComp))
        {
            if (gameRuleComp.ActivatedAt <= _timing.CurTime && !HasComp<EndedGameRuleComponent>(ev))
            {
                isPower = true;
                return true;
            }
        }

        var solarFlareQuery = EntityQueryEnumerator<SolarFlareRuleComponent, GameRuleComponent>();
        while (solarFlareQuery.MoveNext(out var ev, out _, out var gameRuleComp))
        {
            if (gameRuleComp.ActivatedAt <= _timing.CurTime && !HasComp<EndedGameRuleComponent>(ev))
            {
                isSolar = true;
                return true;
            }
        }

        return false;
    }

    private void DistortAllMessages(NanoChatCartridgeComponent comp)
    {
        foreach (var chatKey in comp.Messages.Keys.ToList())
        {
            var distortedMessages = new List<ChatMessage>();

            foreach (var message in comp.Messages[chatKey])
            {
                var distortedText = DistortMessage(message.Message);
                var distortedMessage = new ChatMessage(
                    message.SenderId,
                    message.SenderName,
                    distortedText,
                    message.Timestamp,
                    message.IsOwnMessage,
                    message.Delivered
                );

                distortedMessages.Add(distortedMessage);
            }

            comp.Messages[chatKey] = distortedMessages;
        }

        foreach (var contactKey in comp.Contacts.Keys.ToList())
        {
            var contact = comp.Contacts[contactKey];
            var distortedName = DistortMessage(contact.ContactName);

            comp.Contacts[contactKey] = new ChatContact(
                contact.ContactId,
                distortedName,
                contact.HasUnread
            );
        }

        foreach (var groupKey in comp.Groups.Keys.ToList())
        {
            var group = comp.Groups[groupKey];
            var distortedName = DistortMessage(group.GroupName);

            comp.Groups[groupKey] = new ChatGroup(
                group.GroupId,
                distortedName,
                group.HasUnread,
                group.MemberCount
            );
        }
    }

    private string DistortMessage(string originalMessage)
    {
        if (string.IsNullOrEmpty(originalMessage))
            return originalMessage;

        const string distortionChars = "~!@#$%^&*()_+-=[]{}|;:,.<>?/";
        var result = new char[originalMessage.Length];
        var distortionStrength = 0.7f;

        for (int i = 0; i < originalMessage.Length; i++)
        {
            if (_random.Prob(distortionStrength))
            {
                result[i] = distortionChars[_random.Next(distortionChars.Length)];
            }
            else
            {
                result[i] = originalMessage[i];
            }
        }

        return new string(result);
    }

    private sealed class ChatGroupData
    {
        public string GroupId = string.Empty;
        public string GroupName = string.Empty;
        public HashSet<string> Members = new();
        public List<ChatMessage> Messages = new();
    }
}
