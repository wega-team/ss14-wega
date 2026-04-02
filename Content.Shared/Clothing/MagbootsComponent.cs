using Content.Shared.Alert;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)] // Corvax-Wega-AdvMagboots-Edit
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

    // Corvax-Wega-AdvMagboots-start
    [DataField, AutoNetworkedField]
    public bool DisabledAutoMode = false;

    [DataField, AutoNetworkedField]
    public bool DisabledAutoOff = false;
    // Corvax-Wega-AdvMagboots-end

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
