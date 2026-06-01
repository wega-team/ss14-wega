using Robust.Shared.GameObjects;

namespace Content.Server.Ninja.Components;

/// <summary>
/// Tracks uranium sheets stored in the ninja suit's fuel tank for the adrenaline burst ability.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaUraniumTankComponent : Component
{
    [DataField]
    public int UraniumStored;

    [DataField]
    public int UraniumMax = 30;

    [DataField]
    public int UraniumBurstCost = 15;
}
