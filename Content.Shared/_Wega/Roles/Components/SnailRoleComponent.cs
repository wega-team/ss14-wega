using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SnailImmortalRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public EntProtoId SnailKillRandomPersonObjective = "SnailKillRandomPersonObjective";
}
