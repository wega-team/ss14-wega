using Content.Shared.Lavaland;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class StationLavalandComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<LavalandPlanetPrototype>> Planets = new();

    [DataField] public bool Enabled = true;

    [DataField("avanposts")]
    public Dictionary<ProtoId<LavalandPlanetPrototype>, List<ResPath>> PlanetAvanposts = new();

    [DataField("defaultAvanpost", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath DefaultAvanpostPath { get; set; } = new("/Maps/_Wega/Nonstations/base_lavalandavanpost.yml");
}

[RegisterComponent]
public sealed partial class LavalandAvanpostComponent : Component
{
    [DataField] public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Supply";
}
