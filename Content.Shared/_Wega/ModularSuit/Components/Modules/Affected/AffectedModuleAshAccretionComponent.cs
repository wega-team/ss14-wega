using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

/// <summary>
/// Бонусы от пепельного покрытия,
/// работает пока <see cref="Engaged"/> == true.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AffectedModuleAshAccretionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageModifierSet Modifiers = new();

    [DataField, AutoNetworkedField] public float WalkModifier = 1f;

    [DataField, AutoNetworkedField] public float SprintModifier = 1f;

    [DataField, AutoNetworkedField] public float ExplosionCoefficient = 1f;

    [ViewVariables, AutoNetworkedField] public bool Engaged;
}
