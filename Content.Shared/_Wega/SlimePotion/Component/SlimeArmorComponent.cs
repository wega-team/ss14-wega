using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeArmorComponent : Component
{
	/// при 0 не имеет лиммитов
    [DataField]
    public int Uses = 1;

    [DataField]
    public float Delay = 3.0f;
	
	[DataField, AutoNetworkedField]
    public DamageModifierSet Modifiers = new();
}