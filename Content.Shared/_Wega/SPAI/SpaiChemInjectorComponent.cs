using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.SPAI;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpaiChemInjectorComponent : Component
{
    [DataField]
    public int Energy = 30;

    [DataField]
    public int EnergyMax = 30;

    [DataField]
    public int EnergyPerRefill = 5;

    [DataField]
    public float RefillIntervalSeconds = 15f;

    [ViewVariables]
    public float RefillAccumulator = 0f;
}
