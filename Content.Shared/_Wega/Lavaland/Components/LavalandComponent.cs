using Content.Shared.Weather;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
[Access(typeof(SharedLavalandSystem))]
public sealed partial class LavalandComponent : Component
{
    [DataField]
    public ProtoId<LavalandPlanetPrototype> PlanetPrototype { get; set; } = "Lavaland";

    [DataField]
    public TimeSpan NextWeatherChange = TimeSpan.Zero;

    [DataField]
    public TimeSpan WeatherStartTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan CurrentWeatherEnd = TimeSpan.Zero;

    [DataField]
    public LavalandWeatherType UpcomingWeatherType = LavalandWeatherType.None;

    [DataField]
    public EntProtoId? UpcomingWeatherProto;

    [DataField]
    public LavalandWeatherType CurrentWeatherType = LavalandWeatherType.None;

    [DataField]
    public EntProtoId? CurrentWeatherProto;

    [DataField]
    public bool WarningSent = false;

    [DataField]
    public float DamageTick = 0f;

    [DataField] public SoundSpecifier RumbleSound = new SoundCollectionSpecifier("PlanetRumblesDistance");
    [DataField] public SoundSpecifier RockFallSound = new SoundCollectionSpecifier("Explosion");
}
