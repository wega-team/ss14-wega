using Content.Shared.Chat;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of sending a message to the in-game chat (IC).
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SendMessageEffect : InteractionEffect
{
    /// <summary>
    /// A localized message to be sent.
    /// </summary>
    [DataField(required: true)]
    public LocId Message { get; private set; } = default!;

    /// <summary>
    /// The sender of the message: the user, the target, or both. <see cref="EffectTarget"/>
    /// </summary>
    [DataField]
    public EffectTarget Sender { get; private set; } = EffectTarget.User;

    /// <summary>
    /// The type of IC chat. <see cref="InGameICChatType"/>
    /// </summary>
    [DataField]
    public InGameICChatType Chat { get; private set; } = InGameICChatType.Emote;

    /// <summary>
    /// Hide the message in the local chat.
    /// </summary>
    [DataField]
    public bool HideMessage { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var entity = Sender == EffectTarget.User ? user : target;

        var chatSystem = entityManager.System<SharedChatSystem>();
        chatSystem.TrySendInGameICMessage(entity, Loc.GetString(Message, ("user", Identity.Name(user, entityManager, entity)),
                ("target", Identity.Name(target, entityManager, entity))), Chat, HideMessage);
    }
}
