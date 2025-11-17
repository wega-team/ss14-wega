using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MagbootsImplantComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> MagbootsAlert = "Magboots";

    [DataField]
    public string ToggleAction = "ActionToggleBodyPartImplantMagboots";
    [ViewVariables]
    public EntityUid? ToggleActionEntity;

    [DataField, AutoNetworkedField]
    public bool Toggled = false;
}
