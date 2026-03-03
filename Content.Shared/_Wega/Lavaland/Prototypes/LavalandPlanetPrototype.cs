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

    [DataField]
    public Color MapLightColor = Color.FromHex("#4D4033");

    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> BiomeLayers = new();

    [DataField]
    public List<LavalandWeatherType> AvailableWeather = new()
    {
        LavalandWeatherType.StormWind,
        LavalandWeatherType.AshStormLight,
        LavalandWeatherType.AshStormHeavy,
        LavalandWeatherType.VolcanicActivity,
        LavalandWeatherType.AcidRain
    };

    [DataField("temperature")]
    public float AtmosphereTemperature = 293.15f;

    [DataField("gases")]
    public float[] GasesContent = new float[Atmospherics.TotalNumberOfGases];
}

public enum LavalandWeatherType : byte
{
    None = 0,
    AshStormLight,
    AshStormHeavy,
    VolcanicActivity,
    AcidRain,
    StormWind
}
