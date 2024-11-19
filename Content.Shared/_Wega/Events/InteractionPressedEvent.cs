using Robust.Shared.Serialization;

namespace Content.Shared.Interaction
{
    [Serializable, NetSerializable]
    public sealed class InteractionPressedEvent : EntityEventArgs
    {
        public NetEntity User { get; }
        public string InteractionId { get; }
        public NetEntity? Target { get; }

        public InteractionPressedEvent(NetEntity user, string interactionId, NetEntity? target)
        {
            User = user;
            InteractionId = interactionId;
            Target = target;
        }
    }
}
