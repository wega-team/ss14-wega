using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of displaying localized messages to different parties.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class PopupMessageEffect : InteractionEffect
{
    /// <summary>
    /// Message for the initiator. Localized identifier.
    /// </summary>
    [DataField]
    public LocId? UserMessage { get; private set; } = default!;

    /// <summary>
    /// Message for the addressee. Localized identifier.
    /// </summary>
    [DataField]
    public LocId? TargetMessage { get; private set; } = default!;

    /// <summary>
    /// A message for the others around.
    /// </summary>
    [DataField]
    public LocId? OtherMessage { get; private set; } = default!;

    /// <summary>
    /// Message/chat color. Example: <see cref="Color.Red"/> be careful.
    /// </summary>
    [DataField]
    public Color? ColorMessage { get; private set; } = default!;

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var popupSystem = entityManager.System<SharedPopupSystem>();
        var chatSystem = entityManager.System<SharedChatSystem>();

        if (!string.IsNullOrWhiteSpace(UserMessage))
        {
            var message = Loc.GetString(UserMessage, ("user", Identity.Name(user, entityManager, user)),
                ("target", Identity.Name(target, entityManager, user)));

            var popupType = ColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
            popupSystem.PopupEntity(message, user, user, popupType);
            chatSystem.SendMessageToOne(user, message, ColorMessage);
        }

        if (!string.IsNullOrWhiteSpace(TargetMessage))
        {
            var message = Loc.GetString(TargetMessage, ("user", Identity.Name(user, entityManager, target)),
                ("target", Identity.Name(target, entityManager, target)));

            var popupType = ColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
            popupSystem.PopupEntity(message, target, target, popupType);
            chatSystem.SendMessageToOne(target, message, ColorMessage);
        }

        if (!string.IsNullOrWhiteSpace(OtherMessage))
        {
            var filter = Filter.Local().RemoveWhereAttachedEntity(uid => uid == user)
                .RemoveWhereAttachedEntity(uid => uid == target);

            var popupType = ColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
            popupSystem.PopupEntity(Loc.GetString(OtherMessage, ("user", Identity.Name(user, entityManager)),
                ("target", Identity.Name(target, entityManager))), target, filter, false, popupType);
        }
    }
}
