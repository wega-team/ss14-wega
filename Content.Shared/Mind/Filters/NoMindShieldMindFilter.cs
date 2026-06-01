using Content.Shared.Mindshield.Components;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// Removes minds whose owned entity has a <see cref="MindShieldComponent"/> implanted.
/// </summary>
public sealed partial class NoMindShieldMindFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan)
    {
        if (mind.Comp.OwnedEntity is not { } entity)
            return true;
        return entMan.HasComponent<MindShieldComponent>(entity);
    }
}
