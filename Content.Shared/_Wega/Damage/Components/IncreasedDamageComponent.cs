using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class IncreasedDamageComponent : Component
{
    [DataField]
    public float DamageModifier = 0.1f;

    [DataField]
    public TimeSpan ActiveInterval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan EndTime;
}
