using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared.Interaction;

/// <summary>
/// The target must have a label from the specified categories.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class MarkingPresentCondition : InteractionCondition
{
    /// <summary>
    /// Marking categories for verification. <see cref="MarkingCategories"/>
    /// </summary>
    [DataField(required: true)]
    public MarkingCategories Categories { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoid))
            return false;

        return humanoid.MarkingSet.TryGetCategory(Categories, out _);
    }
}
