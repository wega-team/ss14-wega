using Content.Shared.Alert;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MedicalAuraComponent : Component
{
    [DataField]
    public float NextTimeTick { get; set; }
	
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
	
    [DataField]
    public DamageSpecifier? Heal;

    [DataField]
    public float Radius = 3.5f;
	
    [DataField]
    public float Blood = 20f;
	
    [DataField]
    public float Bleed = -2.0f;
	
    [DataField]
    public ProtoId<AlertPrototype>? Alert = "HealAura";
	
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1f);
}
