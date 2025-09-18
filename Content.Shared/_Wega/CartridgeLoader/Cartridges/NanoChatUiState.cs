using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NanoChatUiState : BoundUserInterfaceState
{
    public string ChatId;
    public string? ActiveChat;
    public bool Muted;
    public Dictionary<string, ChatContact> Contacts;
    public Dictionary<string, ChatGroup> Groups;
    public List<ChatMessage>? ActiveChatMessages;

    public NanoChatUiState(
        string chatId, string? activeChat, bool muted,
        Dictionary<string, ChatContact> contacts,
        Dictionary<string, ChatGroup> groups,
        List<ChatMessage>? activeChatMessages
    )
    {
        ChatId = chatId;
        ActiveChat = activeChat;
        Muted = muted;
        Contacts = contacts;
        Groups = groups;
        ActiveChatMessages = activeChatMessages;
    }
}

[Serializable, NetSerializable]
public sealed class ChatContact
{
    public string ContactId { get; }
    public string ContactName { get; }
    public bool HasUnread { get; }

    public ChatContact(string contactId, string contactName, bool hasUnread)
    {
        ContactId = contactId;
        ContactName = contactName;
        HasUnread = hasUnread;
    }
}

[Serializable, NetSerializable]
public sealed class ChatGroup
{
    public string GroupId { get; }
    public string GroupName { get; }
    public bool HasUnread { get; }
    public int MemberCount { get; }

    public ChatGroup(string groupId, string groupName, bool hasUnread, int memberCount)
    {
        GroupId = groupId;
        GroupName = groupName;
        HasUnread = hasUnread;
        MemberCount = memberCount;
    }
}

[Serializable, NetSerializable]
public sealed class ChatMessage
{
    public string SenderId { get; }
    public string SenderName { get; }
    public string Message { get; }
    public TimeSpan Timestamp { get; }
    public bool IsOwnMessage { get; }
    public bool Delivered { get; }

    public ChatMessage(string senderId, string senderName, string message, TimeSpan timestamp, bool isOwnMessage, bool delivered)
    {
        SenderId = senderId;
        SenderName = senderName;
        Message = message;
        Timestamp = timestamp;
        IsOwnMessage = isOwnMessage;
        Delivered = delivered;
    }
}
