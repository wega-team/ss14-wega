using Content.Server.Lavaland.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Lavaland.Components;

[RegisterComponent, Access(typeof(LavalandShuttleSystem))]
public sealed partial class StationLavalandShuttleComponent : Component
{
    [DataField]
    public EntityUid? LavalandShuttle;

    [DataField("lavalandShuttlePath", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath LavalandShuttlePath { get; set; } = new("/Maps/_Wega/Shuttles/base_lavalandshuttle.yml");
}
