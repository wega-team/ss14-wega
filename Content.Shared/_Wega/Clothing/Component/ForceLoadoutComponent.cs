using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForceLoadoutComponent : Component
{
    [DataField("prototypes")]
    [AutoNetworkedField]
    public List<ProtoId<StartingGearPrototype>>? StartingGear;
}
