using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public abstract partial class ToggleableHudComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public EntProtoId ToggleAction = "ActionToggleHud";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
	
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound;
	
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivateSound;
}
