using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland;

[Prototype]
public sealed partial class LavalandPlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome = default!;

    // If null, its random
    [DataField] public int? Seed = null;

    [DataField]
    public Color MapLightColor = Color.FromHex("#4D4033");

    [DataField]
    public float MaxLightLevel = 3f;

    [DataField]
    public float MinLightLevel = 0.2f;

    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> BiomeLayers = new();

    [DataField("weather")]
    public List<ProtoId<LavalandWeatherEntryPrototype>> AvailableWeather = new();

    [DataField("temperature")]
    public float AtmosphereTemperature = 293.15f;

    [DataField("gases")]
    public float[] GasesContent = new float[Atmospherics.TotalNumberOfGases];
}

/// <summary>
/// Keys for weather phenomena that have unique effects from their activity.
/// </summary>
public enum LavalandWeatherType : byte
{
    None = 0,
    VolcanicActivity,
    StormWind
}
