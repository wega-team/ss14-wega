using Robust.Shared.GameStates;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OreProcessorPointsComponent : Component
{
    [DataField("accumulatedPoints")]
    public double AccumulatedPoints = 0;

    [DataField("pointMultipliers")]
    public Dictionary<string, float> PointMultipliers = new()
    {
        { "DiamondOre", 50.0f },
        { "BananiumOre", 60.0f },
        { "UraniumOre", 30.0f },
        { "PlasmaOre", 15.0f },
        { "GoldOre", 18.0f },
        { "SilverOre", 16.0f },
        { "SteelOre", 1.0f },
        { "SpaceQuartz", 0.0f },
        { "Coal", 0.0f }
    };
}