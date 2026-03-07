using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Allows for hitscan entities to ignite their targets. This component modifies the ignition chance, as well as how many stacks are added once ignited.
/// </summary>
[RegisterComponent, AutoGenerateComponentState(true)]
public sealed partial class HitscanEMPComponent : Component
{
    /// <summary>
    /// EMP range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1.0f;

    /// <summary>
    /// How much energy (in Joules) will be consumed per battery in range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DisableDuration = TimeSpan.FromSeconds(10);
}