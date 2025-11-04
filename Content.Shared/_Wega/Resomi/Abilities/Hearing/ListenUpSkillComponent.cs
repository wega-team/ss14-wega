using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Resomi.Abilities.Hearing;

[RegisterComponent]
public sealed partial class ListenUpSkillComponent : Component
{

    [DataField("switchListenUpAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SwitchListenUpAction = "SwitchListenUpAction";

    [DataField]
    public EntityUid? SwitchListenUpActionEntity;

    [DataField]
    public bool Toggled = false;

    [DataField]
    public float PrepareTime = 3f;
}
