using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Garrotte;

[RegisterComponent, NetworkedComponent]
public sealed partial class GarrotteComponent : Component
{
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier GarrotteDamage = default!;
}
