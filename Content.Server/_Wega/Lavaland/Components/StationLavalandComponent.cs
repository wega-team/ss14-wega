using Content.Shared.Lavaland;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class StationLavalandComponent : Component
{
    [DataField]
    public List<ProtoId<LavalandPlanetPrototype>> Planets = new() {
        "Lavaland",
    };

    // If null, its random
    [DataField] public int? Seed = null;

    [DataField("lavalandAvanpost", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath LavalandAvanpostPath { get; set; } = new("/Maps/_Wega/Nonstations/base_lavalandavanpost.yml");
}

[RegisterComponent]
public sealed partial class LavalandAvanpostComponent : Component
{
    [DataField] public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Supply";
}
