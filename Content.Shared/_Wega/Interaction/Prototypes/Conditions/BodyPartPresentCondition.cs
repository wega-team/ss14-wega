using Content.Shared.Body;
using Robust.Shared.Prototypes;

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
    public ProtoId<OrganCategoryPrototype> BodyPart { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<BodyComponent>(target, out var bodyComp) || bodyComp.Organs == null)
            return false;

        foreach (var organ in bodyComp.Organs.ContainedEntities)
        {
            if (entityManager.TryGetComponent<OrganComponent>(organ, out var organComp)
                && organComp.Category == BodyPart)
            {
                return true;
            }
        }

        return false;
    }
}
