using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks that the target is not standing and supports the flip.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class KnockdownCondition : InteractionCondition
{
    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.HasComponent<CrawlerComponent>(target))
            return false;

        return entityManager.TryGetComponent<StandingStateComponent>(target, out var standing) && !standing.Standing;
    }
}
