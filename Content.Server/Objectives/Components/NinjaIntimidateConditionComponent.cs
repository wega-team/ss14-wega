using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition: deal accumulated damage to the target without killing them.
/// Depends on <see cref="TargetObjectiveComponent"/> for the target.
/// A <see cref="NinjaIntimidateTrackerComponent"/> is added to the target's entity on assignment.
/// </summary>
[RegisterComponent, Access(typeof(NinjaNewObjectivesSystem))]
public sealed partial class NinjaIntimidateConditionComponent : Component
{
    [DataField]
    public float MinDamage = 80f;

    [DataField]
    public float MaxDamage = 200f;

    /// <summary>
    /// Damage amount to deal. Randomised from MinDamage–MaxDamage on assignment.
    /// </summary>
    [DataField]
    public float DamageThreshold = 100f;

    /// <summary>
    /// Running total of positive damage deltas received by the target.
    /// </summary>
    [DataField]
    public float AccumulatedDamage = 0f;

    /// <summary>
    /// Owned entity of the target, cached for cleanup on shutdown.
    /// </summary>
    [DataField]
    public EntityUid? TrackedEntity;
}
