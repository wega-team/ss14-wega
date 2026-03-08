using Content.Shared.Humanoid;
using Robust.Shared.Enums;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks whether the target belongs to one of the specified genders.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class GenderConditon : InteractionCondition
{
    /// <summary>
    /// List of acceptable genders.
    /// </summary>
    [DataField(required: true)]
    public List<Gender> Genders { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoid))
            return false;

        return Genders.Contains(humanoid.Gender);
    }
}
