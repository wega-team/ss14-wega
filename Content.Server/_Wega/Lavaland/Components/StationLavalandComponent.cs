using Content.Shared.Parallax.Biomes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class StationLavalandComponent : Component
{
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome = "Lavaland";

    [DataField]
    public Color MapLightColor = Color.FromHex("#4D4033");

    // If null, its random
    [DataField]
    public int? Seed = null;

    [DataField("lavalandAvanpostPath", customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath LavalandAvanpostPath { get; set; } = new("/Maps/_Wega/lava_avanpost.yml");
}
