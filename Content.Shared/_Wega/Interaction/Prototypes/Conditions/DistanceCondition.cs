namespace Content.Shared.Interaction;

/// <summary>
/// The maximum distance between the user and the target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class DistanceCondition : InteractionCondition
{
    /// <summary>
    /// Maximum allowed distance.
    /// </summary>
    [DataField]
    public float MaxRange { get; private set; } = 2f;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var xform = entityManager.GetComponent<TransformComponent>(user);
        var targetXform = entityManager.GetComponent<TransformComponent>(target);

        var distance = (xform.Coordinates.Position - targetXform.Coordinates.Position).Length();

        return distance <= MaxRange;
    }
}
