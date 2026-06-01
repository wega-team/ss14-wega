using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Tracks the active state and stored physics data for the ninja Spirit Form ability.
/// Placed on the ninja suit entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class SpiritFormComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Fraction of max battery charge drained per second while active.
    /// </summary>
    [DataField]
    public float DrainRate = 0.06f;

    /// <summary>
    /// Movement speed multiplier while Spirit Form is active. 0.7 = 30% slower.
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.7f;

    /// <summary>
    /// Whether we added PacifiedComponent ourselves (so we know to remove it on deactivation).
    /// </summary>
    public bool AddedPacifism;

    [DataField]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(25);

    [DataField]
    public string CooldownDelayId = "spirit_form_cooldown";

    /// <summary>
    /// Saved collision mask to restore on deactivation.
    /// </summary>
    public int? OriginalCollisionMask;

    /// <summary>
    /// Saved collision layer to restore on deactivation.
    /// </summary>
    public int? OriginalCollisionLayer;
}
