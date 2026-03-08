using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

/// <summary>
/// Checks whether the specified entity is a specific prototype from the list.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpecificEntityCondition : InteractionCondition
{
    /// <summary>
    /// A list of entity prototype IDs that match the condition.
    /// </summary>
    [DataField("entities", required: true)]
    public List<EntProtoId> EntitiesProto { get; private set; }

    /// <summary>
    /// Which entity to check: user or target. <see cref="ConditionTarget"/>
    /// </summary>
    [DataField]
    public ConditionTarget Target { get; private set; } = ConditionTarget.Target;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var checkEntity = Target == ConditionTarget.User ? user : target;
        if (!entityManager.TryGetComponent<MetaDataComponent>(checkEntity, out var meta)
            || meta.EntityPrototype == null)
            return false;

        return EntitiesProto.Contains(meta.EntityPrototype.ID);
    }
}
