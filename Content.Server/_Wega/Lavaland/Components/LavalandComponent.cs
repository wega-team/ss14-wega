using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandComponent : Component
{
    [DataField]
    public TimeSpan NextStormTime = TimeSpan.Zero;

    [DataField]
    public float DamageTick = 0f;

    [DataField]
    public float StormSeverity = 0f; // 0-1: 0.3=Light, 0.5=Normal, 0.9=Heavy

    [DataField]
    public ProtoId<WeatherPrototype> CurrentStormProto = "AshfallLight";
}