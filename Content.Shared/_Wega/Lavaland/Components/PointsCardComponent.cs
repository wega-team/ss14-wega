using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(UtilityVendorSystem), typeof(OreProcessorPointsSystem))]
public sealed partial class PointsCardComponent : Component
{
    [DataField("points"), AutoNetworkedField]
    public FixedPoint2 Points = 0;
}
