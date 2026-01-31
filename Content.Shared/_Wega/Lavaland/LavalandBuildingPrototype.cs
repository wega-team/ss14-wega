using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared.Lavaland;

[Prototype("lavalandBuilding")]
public sealed partial class LavalandBuildingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("gridPath", required: true, customTypeSerializer: typeof(ResPathSerializer))]
    public ResPath GridPath = default!;

    [DataField("position")]
    public Vector2? ExactPosition = null;

    [DataField("approximate")]
    public (float Min, float Max)? ApproximatePosition = null;

    [DataField("merge")]
    public bool MergeWithPlanet = true;
}
