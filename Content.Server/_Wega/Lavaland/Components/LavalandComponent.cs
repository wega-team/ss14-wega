using Content.Server.Lavaland.Systems;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
[Access(typeof(LavalandSystem))]
public sealed partial class LavalandComponent : Component
{
    [DataField]
    public TimeSpan NextWeatherChange = TimeSpan.Zero;

    [DataField]
    public TimeSpan WeatherStartTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan CurrentWeatherEnd = TimeSpan.Zero;

    [DataField]
    public LavalandWeatherType UpcomingWeatherType = LavalandWeatherType.None;

    [DataField]
    public ProtoId<WeatherPrototype>? UpcomingWeatherProto;

    [DataField]
    public LavalandWeatherType CurrentWeatherType = LavalandWeatherType.None;

    [DataField]
    public ProtoId<WeatherPrototype>? CurrentWeatherProto;

    [DataField]
    public bool WarningSent = false;

    [DataField]
    public float DamageTick = 0f;
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
