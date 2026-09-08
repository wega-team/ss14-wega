using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the occasional sneeze or cough.
/// </summary>
[RegisterComponent]
public sealed partial class UncontrollableSnoughComponent : Component
{
    /// <summary>
    /// Emote to play when snoughing
    /// </summary>
    [DataField("emote")]
    public ProtoId<EmotePrototype> EmoteId = string.Empty;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; set; }

    public float NextIncidentTime;
}
