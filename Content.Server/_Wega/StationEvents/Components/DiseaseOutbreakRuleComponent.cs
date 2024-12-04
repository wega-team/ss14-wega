using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(DiseaseOutbreakRule))]
public sealed partial class DiseaseOutbreakRuleComponent : Component
{
    /// <summary>
    /// Disease prototypes I decided were not too deadly for a random event
    /// </summary>
    [DataField("notTooSeriousDiseases")]
    public IReadOnlyList<string> NotTooSeriousDiseases { get; private set; } = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus",
        "VentCough",
        "AMIV",
        "SpaceFlu",
        "BirdFlew",
        "TongueTwister"
    };
}
