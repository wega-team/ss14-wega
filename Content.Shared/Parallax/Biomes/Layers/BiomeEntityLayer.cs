using Content.Shared.Maps;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Parallax.Biomes.Layers;

[Serializable, NetSerializable]
public sealed partial class BiomeEntityLayer : IBiomeWorldLayer
{
    /// <inheritdoc/>
    [DataField]
    public List<ProtoId<ContentTileDefinition>> AllowedTiles { get; private set; } = new();

    [DataField("noise")] public FastNoiseLite Noise { get; private set; } = new(0);

    /// <inheritdoc/>
    [DataField("threshold")]
    public float Threshold { get; private set; } = 0.5f;

    /// <inheritdoc/>
    [DataField("invert")] public bool Invert { get; private set; } = false;

    [DataField("entities")] // Corvax-Wega-Lavaland-Edit
    public List<EntProtoId> Entities = new();

    // Corvax-Wega-Lavaland-start
    [DataField("entityWeights")]
    public Dictionary<EntProtoId, float> EntityWeights { get; private set; } = new();
    // Corvax-Wega-Lavaland-end
}
