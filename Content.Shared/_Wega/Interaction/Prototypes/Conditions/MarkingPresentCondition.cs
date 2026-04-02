using Content.Shared.Body;
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
    /// Visual layer for verification. <see cref="HumanoidVisualLayers"/>
    /// </summary>
    [DataField(required: true)]
    public HumanoidVisualLayers Layer { get; private set; }

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var visualSystem = entityManager.System<SharedVisualBodySystem>();
        if (!visualSystem.TryGatherMarkingsData(target, null, out _, out _, out var applied))
            return false;

        // Check if any organ has markings on the specified layer
        foreach (var organMarkings in applied.Values)
        {
            if (organMarkings.TryGetValue(Layer, out var markings) && markings.Count > 0)
                return true;
        }

        return false;
    }
}
