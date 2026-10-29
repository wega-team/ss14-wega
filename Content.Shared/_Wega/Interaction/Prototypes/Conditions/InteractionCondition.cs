namespace Content.Shared.Interaction;

/// <summary>
/// The base class for the conditions of interactions.
/// The heirs implement the <see cref="Check"/> method, which checks the condition.
/// The <see cref="Invert"/> field allows you to invert the result.
/// </summary>
[Serializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class InteractionCondition
{
    /// <summary>
    /// Invert the result of checking the condition.
    /// </summary>
    [DataField]
    public bool Invert { get; private set; } = false;

    public bool CheckWithInvert(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var result = Check(user, target, entityManager);
        return Invert ? !result : result;
    }

    /// <summary>
    /// Check the condition for the pair.
    /// </summary>
    public abstract bool Check(EntityUid user, EntityUid target, IEntityManager entityManager);
}
