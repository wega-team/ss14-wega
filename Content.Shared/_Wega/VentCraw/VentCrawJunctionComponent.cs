using Robust.Shared.GameStates;

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")]
    public List<Angle> Degrees = new();
}
