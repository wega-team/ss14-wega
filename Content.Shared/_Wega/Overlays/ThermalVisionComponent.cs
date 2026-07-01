using Content.Shared.Overlay;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Makes the entity see the heat signatures of living creatures: every entity with a
/// <c>MobStateComponent</c> is drawn on top of the world, so they stay visible through
/// walls and in the dark.
/// When added to a clothing item it will also grant the wearer the same vision while worn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ThermalVisionComponent : ToggleableHudComponent;
