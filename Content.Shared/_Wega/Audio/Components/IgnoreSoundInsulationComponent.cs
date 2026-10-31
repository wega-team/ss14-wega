using Robust.Shared.GameStates;

namespace Content.Shared.Audio.Components;

/// <summary>
/// Marks an entity to completely ignore sound insulation and occlusion.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSoundInsulationSystem))]
public sealed partial class IgnoreSoundInsulationComponent : Component;
