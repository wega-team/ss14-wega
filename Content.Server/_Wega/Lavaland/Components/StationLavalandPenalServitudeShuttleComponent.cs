using Content.Server.Lavaland.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Lavaland.Components;

[RegisterComponent, Access(typeof(LavalandPenalServitudeShuttleSystem))]
public sealed partial class StationLavalandPenalServitudeShuttleComponent : Component
{
    [DataField]
    public EntityUid? PenalServitudeShuttle;

    [DataField("shuttlePath", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath PenalServitudeShuttlePath { get; set; } = new("/Maps/_Wega/Shuttles/base_penalservitude_shuttle.yml");
}
