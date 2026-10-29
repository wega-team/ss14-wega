using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of removing labels from the target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class RemoveMarkingEffect : InteractionEffect
{
    /// <summary>
    /// Layers of labels that should be removed. <see cref="HumanoidVisualLayers"/>
    /// </summary>
    [DataField(required: true)]
    public HumanoidVisualLayers Layer { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var visualSystem = entityManager.System<SharedVisualBodySystem>();
        if (!visualSystem.TryGatherMarkingsData(target, null, out _, out _, out var applied))
            return;

        foreach (var organMarkings in applied.Values)
            organMarkings.Remove(Layer);

        visualSystem.ApplyMarkings(target, applied);
    }
}
