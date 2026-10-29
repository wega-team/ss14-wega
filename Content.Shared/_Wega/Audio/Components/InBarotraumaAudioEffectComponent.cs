using Robust.Shared.GameStates;

namespace Content.Shared.Audio.Components;

/// <summary>
/// Marker component indicating that an entity is in a vacuum.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InBarotraumaAudioEffectComponent : Component;
