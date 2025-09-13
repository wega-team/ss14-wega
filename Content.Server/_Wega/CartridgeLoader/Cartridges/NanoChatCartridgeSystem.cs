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

    private void OnRoundRestart(RoundRestartCleanupEvent args) { _activeChats.Clear(); }

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

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnCartridgeRemoved(Entity<NanoChatCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        _activeChats.Remove(ent.Comp.ChatId);
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
        {
            ent.Comp.Contacts.Remove(contactId);
        }

        ent.Comp.ActiveChat = null;
        UpdateUiState(ent);
    }

    private void SwitchMuted(Entity<NanoChatCartridgeComponent> ent)
    {
        ent.Comp.MutedSound = !ent.Comp.MutedSound;
        UpdateUiState(ent);
    }

    private void SetActiveChat(Entity<NanoChatCartridgeComponent> ent, string contactId)
    {
        ent.Comp.ActiveChat = contactId;
        if (ent.Comp.Contacts.TryGetValue(contactId, out var contact))
        {
            ent.Comp.Contacts[contactId] = new ChatContact(contactId, contact.ContactName, false);
        }

        UpdateUiState(ent);
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

    private void UpdateUiState(Entity<NanoChatCartridgeComponent> ent)
    {
        List<ChatMessage>? activeMessages = null;
        if (ent.Comp.ActiveChat != null && ent.Comp.Messages.TryGetValue(ent.Comp.ActiveChat, out var messages))
        {
            activeMessages = messages;
        }

        if (!TryComp<CartridgeComponent>(ent, out var cartridge) || !cartridge.LoaderUid.HasValue)
            return;

        var state = new NanoChatUiState(ent.Comp.ChatId, ent.Comp.ActiveChat, ent.Comp.MutedSound, ent.Comp.Contacts, activeMessages);
        _cartridgeLoader.UpdateCartridgeUiState(cartridge.LoaderUid.Value, state);
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
}
