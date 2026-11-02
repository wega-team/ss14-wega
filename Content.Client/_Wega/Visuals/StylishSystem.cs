using System.Numerics;
using Content.Client.Visuals.Components;
using Content.Shared.CCVar;
using Content.Shared.Visuals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Client.Visuals;

/// <summary>
/// Applies per style visual overrides driven by <see cref="StylishPresetPrototype"/>.
/// Entities only need a <see cref="StylishSpriteComponent"/> marker to participate.
/// </summary>
public sealed partial class StylishSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    /// <summary>
    /// protoId -> { style -> resolved style }. Built once from presets, rebuilt on reload.
    /// </summary>
    private readonly Dictionary<string, Dictionary<StylishType, StyleInfo>> _protoIndex = new();

    private static int _currentVisual;
    private Angle _lastEyeRotation = Angle.Zero;

    #region Lifecycle

    public override void Initialize()
    {
        RebuildIndex();
        Subs.CVar(_cfg, WegaCVars.StylishVisual, OnStyleChanged, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var eye = _eyeManager.CurrentEye;
        var eyeRot = eye.Rotation;
        if (eyeRot.EqualsApprox(_lastEyeRotation, 0.01f))
            return;

        _lastEyeRotation = eyeRot;
        UpdateAllDirectionalOffsets();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<StylishSpriteComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        Apply((ent.Owner, ent.Comp, sprite), _currentVisual);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<StylishSpriteComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        Restore((ent.Owner, ent.Comp, sprite));
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<StylishSpriteComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var protoId = MetaData(ent.Owner).EntityPrototype?.ID;
        if (protoId == null || !_protoIndex.TryGetValue(protoId, out var styles))
            return;

        if (!styles.TryGetValue((StylishType)_currentVisual, out var info))
            return;

        ApplyOffset(ent.Owner, sprite, info);
    }

    #endregion

    #region Index

    /// <summary>
    /// Rebuilds the protoId -> style lookup from every concrete <see cref="StylishPresetPrototype"/>.
    /// Runs on initialize and whenever prototypes are hot-reloaded.
    /// </summary>
    private void RebuildIndex()
    {
        _protoIndex.Clear();

        foreach (var preset in ProtoMan.EnumeratePrototypes<StylishPresetPrototype>())
        {
            if (preset.Abstract)
                continue;

            if (string.IsNullOrEmpty(preset.RsiPath.ToString()) || preset.Entities.Count == 0)
            {
                Log.Error($"StylishPreset '{preset.ID}': RsiPath and Entities are required on concrete presets.");
                continue;
            }

            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSystem.TextureRoot / preset.RsiPath, out var baseRes))
            {
                Log.Error($"StylishPreset '{preset.ID}': failed to load RSI '{preset.RsiPath}'.");
                continue;
            }

            var layers = ResolveLayers(preset);
            var info = new StyleInfo(baseRes.RSI, layers, preset.Rotation, preset.Offsets);

            foreach (var protoId in preset.Entities)
            {
                var key = protoId.Id;

                if (!_protoIndex.TryGetValue(key, out var styles))
                {
                    styles = new Dictionary<StylishType, StyleInfo>();
                    _protoIndex[key] = styles;
                }

                if (!styles.TryAdd(preset.Style, info))
                {
                    Log.Error($"StylishPreset '{preset.ID}': style '{preset.Style}' for entity '{key}' "
                              + $"is already defined by another preset. Duplicate ignored.");
                }
            }
        }
    }

    private Dictionary<object, RSI>? ResolveLayers(StylishPresetPrototype preset)
    {
        if (preset.Layers is not { Count: > 0 })
            return null;

        var layers = new Dictionary<object, RSI>();
        foreach (var (keyString, path) in preset.Layers)
        {
            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSystem.TextureRoot / path, out var layerRes))
            {
                Log.Error($"StylishPreset '{preset.ID}': failed to load layer RSI '{path}' for key '{keyString}'.");
                continue;
            }

            layers[ResolveLayerKey(keyString)] = layerRes.RSI;
        }

        return layers;
    }

    private object ResolveLayerKey(string keyString)
    {
        return _reflection.TryParseEnumReference(keyString, out var @enum) ? @enum : keyString;
    }

    #endregion

    #region Style application

    private void OnStyleChanged(int state)
    {
        _currentVisual = state;

        var query = EntityQueryEnumerator<StylishSpriteComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            Apply((uid, comp, sprite), state);
        }
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<StylishPresetPrototype>())
            return;

        RebuildIndex();

        var query = EntityQueryEnumerator<StylishSpriteComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            comp.SnapshotTaken = false;
            comp.OriginalLayers = null;
            Apply((uid, comp, sprite), _currentVisual);
        }
    }

    private void Apply(Entity<StylishSpriteComponent, SpriteComponent> ent, int state)
    {
        var style = (StylishType)state;
        StyleInfo? info = null;

        if (style != StylishType.Current)
        {
            var protoId = MetaData(ent.Owner).EntityPrototype?.ID;
            if (protoId != null && _protoIndex.TryGetValue(protoId, out var styles))
            {
                styles.TryGetValue(style, out info);
                if (info != null)
                    EnsureSnapshot(ent, styles);
            }
        }

        Restore(ent);
        if (info == null)
            return;

        _sprite.SetBaseRsi((ent.Owner, ent.Comp2), info.Rsi);
        TryApplyRotation(ent.Owner, ent.Comp2, info.Rotation);
        ApplyOffset(ent.Owner, ent.Comp2, info);

        if (info.Layers == null)
            return;

        foreach (var (key, rsi) in info.Layers)
        {
            if (!TryGetLayerByKey(ent.Owner, ent.Comp2, key, out _))
            {
                Log.Error($"StylishSpriteComponent on {ToPrettyString(ent)}: layer key '{key}' (style {style}) not found.");
                continue;
            }

            SetLayerRsi(ent.Owner, ent.Comp2, key, rsi, state: null);
        }
    }

    private void UpdateAllDirectionalOffsets()
    {
        var query = EntityQueryEnumerator<StylishSpriteComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (!comp.SnapshotTaken)
                continue;

            var protoId = MetaData(uid).EntityPrototype?.ID;
            if (protoId == null || !_protoIndex.TryGetValue(protoId, out var styles))
                continue;

            if (!styles.TryGetValue((StylishType)_currentVisual, out var info))
                continue;

            ApplyOffset(uid, sprite, info);
        }
    }

    private void Restore(Entity<StylishSpriteComponent, SpriteComponent> ent)
    {
        if (!ent.Comp1.SnapshotTaken)
            return;

        _sprite.SetBaseRsi((ent.Owner, ent.Comp2), ent.Comp1.OriginalBaseRsi);
        _sprite.SetRotation((ent.Owner, ent.Comp2), ent.Comp1.OriginalRotation);
        _sprite.SetOffset((ent.Owner, ent.Comp2), ent.Comp1.OriginalOffset);

        if (ent.Comp1.OriginalLayers == null)
            return;

        foreach (var (key, snapshot) in ent.Comp1.OriginalLayers)
        {
            if (!TryGetLayerByKey(ent.Owner, ent.Comp2, key, out _))
                continue;

            SetLayerRsi(ent.Owner, ent.Comp2, key, snapshot.Rsi, snapshot.State);
        }
    }

    /// <summary>
    /// Captures the sprite's current state the first time a non-Current style is applied.
    /// Doing it lazily keeps memory flat for entities that never switch styles.
    /// </summary>
    private void EnsureSnapshot(
        Entity<StylishSpriteComponent, SpriteComponent> ent,
        Dictionary<StylishType, StyleInfo> styles)
    {
        var comp = ent.Comp1;
        if (comp.SnapshotTaken)
            return;

        comp.SnapshotTaken = true;
        comp.OriginalBaseRsi = ent.Comp2.BaseRSI;
        comp.OriginalRotation = ent.Comp2.Rotation;
        comp.OriginalOffset = ent.Comp2.Offset;

        // Collect the union of layer keys across every preset this prototype has.
        Dictionary<object, StylishSpriteComponent.LayerSnapshot>? layers = null;
        HashSet<object>? seen = null;

        foreach (var info in styles.Values)
        {
            if (info.Layers == null)
                continue;

            foreach (var key in info.Layers.Keys)
            {
                seen ??= new HashSet<object>();
                if (!seen.Add(key))
                    continue;

                if (!TryGetLayerByKey(ent.Owner, ent.Comp2, key, out var layer))
                    continue;

                layers ??= new Dictionary<object, StylishSpriteComponent.LayerSnapshot>();
                layers[key] = new StylishSpriteComponent.LayerSnapshot(layer.Rsi, layer.RsiState);
            }
        }

        comp.OriginalLayers = layers;
    }

    #endregion

    #region Helpers

    private void TryApplyRotation(EntityUid uid, SpriteComponent sprite, Angle? target)
    {
        if (target == null)
            return;

        if (sprite.NoRotation)
            return;

        var xform = Transform(uid);
        if (!xform.Anchored || xform.NoLocalRotation)
            return;

        _sprite.SetRotation((uid, sprite), target.Value - xform.LocalRotation);
    }

    private void ApplyOffset(EntityUid uid, SpriteComponent sprite, StyleInfo info)
    {
        if (info.Offsets is not { Count: > 0 })
            return;

        // If only one offset is provided, apply it statically.
        if (info.Offsets.Count == 1)
        {
            _sprite.SetOffset((uid, sprite), info.Offsets[0]);
            return;
        }

        // Otherwise, use the directional logic.
        var eye = _eyeManager.CurrentEye;
        var eyeRot = eye?.Rotation ?? Angle.Zero;
        var xform = Transform(uid);

        var totalRot = xform.WorldRotation - eyeRot;
        var offset = GetDirectionalOffset(info.Offsets, totalRot);
        _sprite.SetOffset((uid, sprite), offset);
    }

    /// <summary>
    /// Maps the current rotation to a directional offset from the provided list.
    /// </summary>
    private Vector2 GetDirectionalOffset(List<Vector2> offsets, Angle rotation)
    {
        if (offsets.Count == 1)
            return offsets[0];

        var degrees = rotation.Degrees;
        degrees = (degrees % 360 + 360) % 360;

        // Maps angles to the 4 directions according to the user's definition:
        // North = 0, West = 90, South = 180, East = -90 (270).
        var index = degrees switch
        {
            >= 315 or < 45 => 0,    // North
            >= 45 and < 135 => 3,   // West
            >= 135 and < 225 => 2,  // South
            >= 225 and < 315 => 1,  // East
            _ => 0
        };

        if (index >= offsets.Count)
            return Vector2.Zero;

        return offsets[index];
    }

    private bool TryGetLayerByKey(EntityUid uid, SpriteComponent sprite, object key, out ISpriteLayer layer)
    {
        switch (key)
        {
            case Enum e when _sprite.TryGetLayer((uid, sprite), e, out var l, logMissing: false):
                layer = l;
                return true;

            case string s when _sprite.TryGetLayer((uid, sprite), s, out var l, logMissing: false):
                layer = l;
                return true;

            default:
                layer = default!;
                return false;
        }
    }

    private void SetLayerRsi(EntityUid uid, SpriteComponent sprite, object key, RSI? rsi, RSI.StateId? state)
    {
        switch (key)
        {
            case Enum e:
                _sprite.LayerSetRsi((uid, sprite), e, rsi, state);
                break;
            case string s:
                _sprite.LayerSetRsi((uid, sprite), s, rsi, state);
                break;
        }
    }

    #endregion

    /// <summary>
    /// Resolved style data for one prototype and style pair.
    /// </summary>
    private sealed record StyleInfo(RSI Rsi, Dictionary<object, RSI>? Layers, Angle? Rotation, List<Vector2>? Offsets);
}
