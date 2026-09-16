using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.HangedMan;

/// <summary>
/// Marker for the "Висельница" structure built from cables.
/// Dragging a mob onto it equips the noose cloak and starts the hanging.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HangedManStructureComponent : Component
{
    /// <summary>
    /// The noose cloak that gets equipped onto the victim's neck.
    /// </summary>
    [DataField]
    public EntProtoId Cloak = "ClothingNeckHangedMan";

    /// <summary>
    /// How long it takes to hang someone (or yourself) on the structure.
    /// </summary>
    [DataField]
    public TimeSpan HangDelay = TimeSpan.FromSeconds(5);
}
