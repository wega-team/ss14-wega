using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to the ninja wearer by the suit's ComponentToggler while phase cloak is active.
/// Client uses this to render the ninja fully transparent (no distortion shader).
/// Server uses this to block examination and update heat on the suit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaCloakActiveComponent : Component
{
}
