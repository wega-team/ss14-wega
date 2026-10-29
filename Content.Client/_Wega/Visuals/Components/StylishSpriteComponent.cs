using System.Numerics;
using Content.Shared.Visuals.Components;
using Robust.Client.Graphics;

namespace Content.Client.Visuals.Components;

/// <summary>
/// Marker that opts an entity into the Stylish visual system. Style data itself
/// lives in <see cref="StylishPresetPrototype"/>s and is looked up by prototype ID.
/// </summary>
[RegisterComponent, Access(typeof(StylishSystem))]
public sealed partial class StylishSpriteComponent : SharedStylishSpriteComponent
{
    /// <summary>
    /// True once the original sprite state has been captured.
    /// Restores are skipped while this is false.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool SnapshotTaken;

    /// <summary>
    /// Original BaseRSI of the sprite, captured on the first style change.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public RSI? OriginalBaseRsi;

    /// <summary>
    /// Original local rotation of the sprite, captured on the first style change.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Angle OriginalRotation;

    /// <summary>
    /// Original local offset of the sprite, captured on the first style change.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 OriginalOffset;

    /// <summary>
    /// Original RSI and state per preset-touched layer. Null if the sprite was never
    /// restyled snapshot is lazy so most entities never allocate this.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<object, LayerSnapshot>? OriginalLayers;

    /// <summary>
    /// RSI and state of a layer at snapshot time.
    /// </summary>
    public readonly record struct LayerSnapshot(RSI? Rsi, RSI.StateId State);
}
