using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Temporarily added to an energy-net victim so the client draws a beam back to the caster ninja.
/// Removed by the server after a short duration.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnergyNetBeamComponent : Component
{
    /// <summary>
    /// The ninja that cast the net; the beam is drawn from here to the entity holding this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Caster;
}
