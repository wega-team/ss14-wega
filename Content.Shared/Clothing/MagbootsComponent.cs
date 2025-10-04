using Content.Shared.Alert;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> MagbootsAlert = "Magboots";

    /// <summary>
    /// If true, the user must be standing on a grid or planet map to experience the weightlessness-canceling effect
    /// </summary>
    [DataField]
    public bool RequiresGrid = true;

    [DataField] // Corvax-Wega-AdvMagboots
    public bool DisabledAutoOff = false; // Corvax-Wega-AdvMagboots

    /// <summary>
    /// Slot the clothing has to be worn in to work.
    /// </summary>
    [DataField]
    public string Slot = "shoes";
}

// Corvax-Wega-AdvMagboots-start
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsUserComponent : Component;
// Corvax-Wega-AdvMagboots-end
