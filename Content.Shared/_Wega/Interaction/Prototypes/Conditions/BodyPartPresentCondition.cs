using Content.Shared.Body.Systems;

namespace Content.Shared.Interaction;

/// <summary>
/// The target must have the specified body part.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BodyPartPresentCondition : InteractionCondition
{
    /// <summary>
    /// The identifier of the body part to be checked.
    /// </summary>
    [DataField]
    public string BodyPart { get; private set; } = string.Empty;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<SharedBodySystem>();
        return bodySystem.HasBodyPart(target, BodyPart);
    }
}
