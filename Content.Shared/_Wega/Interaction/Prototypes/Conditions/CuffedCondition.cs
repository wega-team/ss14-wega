using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Shared.Interaction;

/// <summary>
/// The target must be handcuffed/bound.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class CuffedCondition : InteractionCondition
{
    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<CuffableComponent>(target, out var cuffable))
            return false;

        var cuffableSystem = entityManager.System<SharedCuffableSystem>();
        return cuffableSystem.IsCuffed((target, cuffable));
    }
}
