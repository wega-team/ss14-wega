using Content.Shared.Damage.Systems;

namespace Content.Shared.Interaction;

/// <summary>
/// An effect that depletes the target's stamina.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class StaminaDamageEffect : InteractionEffect
{
    /// <summary>
    /// Amount of stamina removed.
    /// Required field.
    /// </summary>
    [DataField(required: true)]
    public float Amount { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var staminaSystem = entityManager.System<SharedStaminaSystem>();
        staminaSystem.TryTakeStamina(target, Amount);
    }
}
