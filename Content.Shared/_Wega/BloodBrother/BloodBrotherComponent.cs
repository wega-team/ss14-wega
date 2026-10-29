using Robust.Shared.GameStates;

namespace Content.Shared.Blood.Brother;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodBrotherComponent : Component
{
    /// <summary>
    /// Mind entity ID of your blood brother
    /// </summary>
    [DataField]
    public EntityUid? BrotherMind;

    /// <summary>
    /// Whether both brothers must survive for victory
    /// </summary>
    [DataField]
    public bool RequireBothAlive = true;

    /// <summary>
    /// Whether both brothers must escape for victory
    /// </summary>
    [DataField]
    public bool RequireBothEscape = true;
}
