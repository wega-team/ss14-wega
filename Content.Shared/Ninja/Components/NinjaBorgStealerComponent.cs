using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to the ninja player when gloves are active with the StealBorg objective.
/// Subscribes BeforeInteractHand to intercept LMB on borgs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaBorgStealerComponent : Component
{
    [DataField]
    public float Delay = 6f;
}
