using Robust.Shared.Audio;
using Content.Shared.Whitelist; // Corvax-Wega-Edit

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class StaminaDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 30f;

    [DataField("sound")]
    public SoundSpecifier? Sound;
	
	// Corvax-Wega-Start
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
	// Corvax-Wega-End
}
