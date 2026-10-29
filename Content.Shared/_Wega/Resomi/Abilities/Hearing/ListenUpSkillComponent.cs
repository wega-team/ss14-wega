using Robust.Shared.Prototypes;

namespace Content.Shared.Resomi.Abilities.Hearing;

[RegisterComponent]
public sealed partial class ListenUpSkillComponent : Component
{
    [DataField("switchListenUpAction")]
    public EntProtoId? SwitchListenUpAction = "SwitchListenUpAction";

    [DataField]
    public EntityUid? SwitchListenUpActionEntity;

    [DataField]
    public bool Toggled = false;

    [DataField]
    public float PrepareTime = 3f;
}
