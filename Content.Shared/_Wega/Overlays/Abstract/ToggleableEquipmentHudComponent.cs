using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Overlays;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public abstract partial class ToggleableHudComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleHud";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public TimeSpan ChargeCheckInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextChargeCheck = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField]
    public SoundSpecifier ActivateFailSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
