using Robust.Shared.GameStates;

namespace Content.Shared.SoundInsolation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SoundInsulationSystem))]
public sealed partial class SoundInsulatorComponent : Component
{
    /// <summary>
    /// The insulation factor.
    /// </summary>
    [DataField("insulationFactor"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float InsulationFactor = 1f;

    /// <summary>
    /// Is this object currently isolating sound?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Isolates = true;
}
