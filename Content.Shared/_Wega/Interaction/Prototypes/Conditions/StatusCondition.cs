using Content.Shared.Humanoid;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks the target's appearance statuses.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class StatusCondition : InteractionCondition
{
    /// <summary>
    /// A list of acceptable statuses.
    /// </summary>
    [DataField(required: true)]
    public List<Status> Statuses { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoid))
            return false;

        return Statuses.Contains(humanoid.Status);
    }
}
