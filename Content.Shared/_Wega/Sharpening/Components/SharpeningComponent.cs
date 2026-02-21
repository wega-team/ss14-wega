using Robust.Shared.GameStates;
using Content.Shared.Damage;

namespace Content.Shared.Sharpening.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SharpeningComponent : Component
{
    [DataField("difficulty"), AutoNetworkedField]
    public int Difficulty = 1;

    [DataField("Uses"), AutoNetworkedField]
    public int Uses = 5;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public DamageSpecifier PreviousDamage = default!;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int UsesRemaining = 0;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Sharpened = false;
}
