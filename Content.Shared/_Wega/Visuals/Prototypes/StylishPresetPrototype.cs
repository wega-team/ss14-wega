using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Visuals;

/// <summary>
/// Maps entity prototype IDs to a per-style visual override.
/// Entities only need a <c>StylishSprite</c> marker; the data lives here.
/// </summary>
[Prototype]
public sealed partial class StylishPresetPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<StylishPresetPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Style this preset applies to. Current is ignored by the system.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public StylishType Style = StylishType.Current;

    /// <summary>
    /// RSI that replaces the entity's BaseRSI.
    /// </summary>
    [DataField]
    [NeverPushInheritance]
    public ResPath RsiPath;

    /// <summary>
    /// Per-layer RSI overrides, keyed by the layer's map value.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, ResPath>? Layers;

    /// <summary>
    /// Optional fixed world angle for the sprite, in degrees.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Angle? Rotation;

    /// <summary>
    /// Optional local offset applied to the sprite.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Vector2? Offset;

    /// <summary>
    /// Entity prototype IDs this preset applies to.
    /// </summary>
    [DataField]
    [NeverPushInheritance]
    public List<ProtoId<EntityPrototype>> Entities = new();
}
