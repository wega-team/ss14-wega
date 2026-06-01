using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Networked marker placed on a borg chassis while it is converted to a ninja borg.
/// The client visualizer reads this to swap the chassis sprite to the ninja skin.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaBorgVisualsComponent : Component { }
