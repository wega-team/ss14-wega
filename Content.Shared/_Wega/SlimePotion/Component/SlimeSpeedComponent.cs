using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSpeedComponent : Component
{
	/// при 0 не имеет лиммитов
    [DataField]
    public int Uses = 1;

    [DataField]
    public float Delay = 3.0f;
}