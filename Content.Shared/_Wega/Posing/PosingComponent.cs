using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Posing;

[Access(typeof(SharedPosingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PosingComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Vector2 CurrentOffset = Vector2.Zero;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Angle CurrentAngle = Angle.Zero;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Posing = false;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetLimits = new(0.3f, 0.3f);

    [DataField, AutoNetworkedField]
    public float AngleLimits = 180f;

    [DataField("defaultInput")]
    public string DefaultInputContext = "human";

    [DataField]
    public Vector2 DefaultOffset = Vector2.Zero;

    [DataField]
    public float DefaultAngle = 0f;
}
