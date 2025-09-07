using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// This entity will take damage from flashes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DamagedByFlashingSystem))]
public sealed partial class DamagedByFlashingComponent : Component
{
    /// <summary>
    /// How much damage it will take.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier FlashDamage = new();

    // Corvax-Wega-Phantom-start
    /// <summary>
    /// Use duration base damage system
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseAdvancedFlashDamage = false;

    /// <summary>
    /// Damage multiplier, only for duration base damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 1f;
    // Corvax-Wega-Phantom-end
}
