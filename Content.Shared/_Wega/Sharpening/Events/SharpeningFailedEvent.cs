namespace Content.Shared.Sharpening.Events;

public sealed class SharpeningFailedEvent : EntityEventArgs
{
    public NetEntity User;

    public SharpeningFailedEvent(NetEntity user)
    {
        User = user;
    }
}