using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

[RegisterComponent, Access(typeof(HeartSystem))]
public sealed partial class HeartComponent : Component
{
    /// <summary>
    /// Next time the heart will beat.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextBeatTime;

    /// <summary>
    /// Interval between heart beats.
    /// </summary>
    [DataField]
    public TimeSpan BeatInterval = TimeSpan.FromSeconds(0.8f);

    /// <summary>
    /// Blood pump efficiency (0-1).
    /// </summary>
    [DataField]
    public float Efficiency = 1.0f;

    /// <summary>
    /// Minimum efficiency to sustain life.
    /// </summary>
    [DataField]
    public float MinEfficiencyForLife = 0.3f;

    /// <summary>
    /// Alert to show when heart is failing.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> HeartFailureAlert = "HeartFailureAlert";

    /// <summary>
    /// The body this heart belongs to.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;
}
