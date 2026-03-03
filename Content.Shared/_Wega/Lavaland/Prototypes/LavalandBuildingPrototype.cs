using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared.Lavaland;

[Prototype]
public sealed partial class LavalandBuildingPrototype : IPrototype
{
    /// <summary>
    /// Seriously..?
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Why should I explain anything? Sosal?
    /// </summary>
    [DataField("gridPath", required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath GridPath = default!;

    /// <summary>
    /// Preloading the grid for a specific position on the map.
    /// </summary>
    [DataField("position")]
    public Vector2? ExactPosition = null;

    /// <summary>
    /// The window within which the building's position will be randomized.
    /// </summary>
    [DataField("approximate")]
    public (float Min, float Max)? ApproximatePosition = null;

    /// <summary>
    /// Should merge the grid to the planet?
    /// </summary>
    [DataField("merge")]
    public bool MergeWithPlanet = true;

    /// <summary>
    /// Whether to ignore the buildings counting. Applies to those constructions that should always be there.
    /// </summary>
    [DataField("ignoring")]
    public bool IgnoringCounting;

    /// <summary>
    /// Is it necessary to preload the spawn area?
    /// Do not preload large grids such as lava rivers and the like if the full grid rectangle is not occupied by space.
    /// </summary>
    [DataField("preloading")]
    public bool PreloadingArea = true;
}
