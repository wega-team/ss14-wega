using Robust.Shared.GameStates;

namespace Content.Shared.Audio.Components;

/// <summary>
/// Marker component indicating that an entity was already processed for dynamic reverb.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DynamicAudioComponent : Component;
