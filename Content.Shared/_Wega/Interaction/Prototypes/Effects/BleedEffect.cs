using Content.Shared.Body.Systems;

namespace Content.Shared.Interaction;

/// <summary>
/// An effect that increases the target's bleeding.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class BleedEffect : InteractionEffect
{
    /// <summary>
    /// Amount of added bleeding.
    /// Specified in the prototype via <see cref="Amount"/>
    /// </summary>
    [DataField(required: true)]
    public float Amount { get; private set; } = 1f;

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var bloodstreamSystem = entityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.TryModifyBleedAmount(target, Amount);
    }
}
