using Robust.Shared.GameStates;

namespace Content.Shared.Crawling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrawlingComponent : Component
{
    /// <summary>
    /// Time it takes to stand up from crawling position
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StandUpTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Speed modifier when crawling
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CrawlingSpeedModifier { get; set; } = 0.35f;

    /// <summary>
    /// Responsible for the absence of collision switching in the crawling state
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCrawling = false;
}
