using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Marker placed on ninja action entities. Tells ActionButton to use the suit's current color
/// for the background sprites instead of static values from ActionComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaAbilityBackgroundComponent : Component;
