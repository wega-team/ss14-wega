using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;

namespace Content.Shared.Garrotte;

[RegisterComponent, NetworkedComponent]
public sealed partial class GarrotteComponent : Component
{
    [DataField]
    public DoAfterId? DoAfterId;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier GarrotteDamage = default!;
}
