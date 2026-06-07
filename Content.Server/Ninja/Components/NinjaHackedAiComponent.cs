using Content.Shared.Silicons.Laws;

namespace Content.Server.Ninja.Components;

/// <summary>
/// Placed on an AI law updater (the AI upload console) after a ninja replaces its laws with
/// ion-storm laws. Any station AI that appears afterwards inherits this lawset, so the sabotage
/// still matters even if no AI was present at the time of the hack.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaHackedAiComponent : Component
{
    [DataField]
    public SiliconLawset? Lawset;
}
