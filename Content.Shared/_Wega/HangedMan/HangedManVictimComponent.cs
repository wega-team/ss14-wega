using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.HangedMan;

/// <summary>
/// Applied to a mob while it is wearing the noose. It cannot move or be pulled,
/// takes asphyxiation damage over time, stands slightly taller and sways.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HangedManVictimComponent : Component
{
    /// <summary>
    /// The noose cloak responsible for this state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Cloak;

    /// <summary>
    /// Damage applied every <see cref="DamageInterval"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = { { "Asphyxiation", 10 } } };

    /// <summary>
    /// How often <see cref="Damage"/> is applied.
    /// </summary>
    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time damage will be applied.
    /// </summary>
    [DataField]
    public TimeSpan NextDamage;

    /// <summary>
    /// Time it takes for the victim to remove the noose itself.
    /// </summary>
    [DataField]
    public TimeSpan SelfRemoveDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Time it takes for somebody else to remove the noose.
    /// </summary>
    [DataField]
    public TimeSpan OtherRemoveDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Maximum sway angle in degrees (client side visual only).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwayAngle = 4f;

    /// <summary>
    /// Sway speed in radians per second (client side visual only).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SwaySpeed = 2.5f;
}
