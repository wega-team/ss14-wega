using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks whether the target belongs to one of the specified races.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpeciesCondition : InteractionCondition
{
    /// <summary>
    /// List of race prototype IDs.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Specieses { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoid))
            return false;

        return Specieses.Contains(humanoid.Species);
    }
}
