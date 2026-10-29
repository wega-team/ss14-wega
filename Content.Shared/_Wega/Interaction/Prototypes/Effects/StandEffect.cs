using Content.Shared.Stunnable;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of putting an entity in a standing state.
/// Used to lift a fallen/staggering target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class StandEffect : InteractionEffect
{
    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var stunSystem = entityManager.System<SharedStunSystem>();
        stunSystem.TryStanding(target);
    }
}
