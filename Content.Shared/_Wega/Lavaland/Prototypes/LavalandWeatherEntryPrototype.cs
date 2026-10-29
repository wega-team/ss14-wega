using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland;

[Prototype("lavalandWeather")]
public sealed partial class LavalandWeatherEntryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LavalandWeatherType? SpecialEffect = default!;

    [DataField]
    public int Weight = 10;

    [DataField]
    public EntProtoId? WeatherPrototype = default!;

    [DataField]
    public float MinDurationSeconds = 60f;

    [DataField]
    public float MaxDurationSeconds = 120f;

    [DataField]
    public float DamageIntervalSeconds = 5f;

    [DataField]
    public DamageSpecifier? Damage = default!;

    [DataField]
    public LocId DamageMessage = "lavaland-weather-damaged-default";

    [DataField]
    public LocId WarningMessage = "lavaland-weather-warning-default";

    [DataField]
    public LocId EndMessage = "lavaland-weather-end-default";
}
