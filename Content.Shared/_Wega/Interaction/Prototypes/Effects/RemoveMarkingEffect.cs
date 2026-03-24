using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of removing labels from the target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class RemoveMarkingEffect : InteractionEffect
{
    /// <summary>
    /// Categories of labels that should be removed. <see cref="MarkingCategories"/>
    /// </summary>
    [DataField(required: true)]
    public MarkingCategories Categories { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        humanoid.MarkingSet.RemoveCategory(Categories);
        entityManager.Dirty(target, humanoid);
    }
}
