namespace Content.Shared.Interaction;

/// <summary>
/// Base class for interaction effects.
/// Subclasses implement the <see cref="Apply"/> method, which is applied to the target/initiator.
/// </summary>
[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class InteractionEffect
{
    /// <summary>
    /// Perform the effect.
    /// </summary>
    public abstract void Apply(EntityUid user, EntityUid target, IEntityManager entityManager);
}
