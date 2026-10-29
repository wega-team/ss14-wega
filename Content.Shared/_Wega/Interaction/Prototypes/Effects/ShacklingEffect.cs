using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of handcuffs/shackles.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ShacklingEffect : InteractionEffect
{
    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<CuffableComponent>(target, out var cuffable))
            return;

        var cuffableSystem = entityManager.System<SharedCuffableSystem>();
        if (cuffableSystem.IsCuffed((target, cuffable)))
            return;

        var handsSystem = entityManager.System<SharedHandsSystem>();
        if (!handsSystem.TryGetActiveItem(user, out var item))
            return;

        cuffableSystem.TryCuffing(user, target, item.Value);
    }
}
