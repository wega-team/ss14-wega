using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Networked marker placed on a borg chassis while the chameleon module disguise is active.
/// The client visualizer swaps the chassis sprite to the service borg skin while this is present.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaBorgChameleonDisguisedComponent : Component { }
