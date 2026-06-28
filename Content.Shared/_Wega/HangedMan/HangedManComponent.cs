using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.HangedMan;

/// <summary>
/// Placed on the noose cloak. When equipped to the neck slot it turns the
/// wearer into a <see cref="HangedManVictimComponent"/>, and when unequipped
/// the cloak deletes itself.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HangedManComponent : Component
{
    /// <summary>
    /// Sound of the cable ties tightening, played when the victim is hung.
    /// </summary>
    [DataField]
    public SoundSpecifier HangSound = new SoundPathSpecifier("/Audio/Items/Handcuffs/rope_start.ogg");
}
