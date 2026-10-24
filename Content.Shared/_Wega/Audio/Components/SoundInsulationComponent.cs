using Robust.Shared.GameStates;

namespace Content.Shared.Audio.Components;

/// <summary>
/// Defines sound blocking and absorption properties for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedSoundInsulationSystem))]
public sealed partial class SoundInsulationComponent : Component
{
    /// <summary>
    /// The fraction of sound energy lost on contact.
    /// Used for both passing and reflecting rays.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AbsorptionFactor = 0.1f;

    /// <summary>
    /// If true the ray reflects instead of passing through.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlocksTransmission;

    /// <summary>
    /// Enables or disables insulation for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Isolates = true;
}
