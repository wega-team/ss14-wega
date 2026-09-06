using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits.Assorted;

[RegisterComponent]
public sealed partial class UncontrollableCoughComponent : Component
{
    [DataField("emote", required: true)]
    public ProtoId<EmotePrototype> EmoteId = string.Empty;

    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; set; }

    public float NextIncidentTime;
}
