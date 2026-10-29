using Content.Shared.Stunnable;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of stunning the target's fall.
/// It puts the target in a prone/stunned state.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class KnockdownEffect : InteractionEffect
{
    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var stunSystem = entityManager.System<SharedStunSystem>();
        stunSystem.TryKnockdown(target, null, autoStand: false);
    }
}
