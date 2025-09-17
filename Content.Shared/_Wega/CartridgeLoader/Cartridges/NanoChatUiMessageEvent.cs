using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

public interface INanoChatUiMessagePayload { }

[Serializable, NetSerializable]
public sealed class NanoChatAddContact : INanoChatUiMessagePayload
{
    public string ContactId { get; }
    public string ContactName { get; }

    public NanoChatAddContact(string contactId, string contactName)
    {
        ContactId = contactId;
        ContactName = contactName;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatEraseContact : INanoChatUiMessagePayload
{
    public string ContactId { get; }

    public NanoChatEraseContact(string contactId)
    {
        ContactId = contactId;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatMuted : INanoChatUiMessagePayload
{
}

[Serializable, NetSerializable]
public sealed class NanoChatSendMessage : INanoChatUiMessagePayload
{
    public string RecipientId { get; }
    public string Message { get; }

    public NanoChatSendMessage(string recipientId, string message)
    {
        RecipientId = recipientId;
        Message = message;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatSetActiveChat : INanoChatUiMessagePayload
{
    public string ContactId { get; }

    public NanoChatSetActiveChat(string contactId)
    {
        ContactId = contactId;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatCreateGroup : INanoChatUiMessagePayload
{
    public string GroupName { get; }

    public NanoChatCreateGroup(string groupName)
    {
        GroupName = groupName;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatJoinGroup : INanoChatUiMessagePayload
{
    public string GroupId { get; }

    public NanoChatJoinGroup(string groupId)
    {
        GroupId = groupId;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatLeaveGroup : INanoChatUiMessagePayload
{
    public string GroupId { get; }

    public NanoChatLeaveGroup(string groupId)
    {
        GroupId = groupId;
    }
}

[Serializable, NetSerializable]
public sealed class NanoChatUiMessageEvent : CartridgeMessageEvent
{
    public readonly INanoChatUiMessagePayload Payload;

    public NanoChatUiMessageEvent(INanoChatUiMessagePayload payload)
    {
        Payload = payload;
    }
}
