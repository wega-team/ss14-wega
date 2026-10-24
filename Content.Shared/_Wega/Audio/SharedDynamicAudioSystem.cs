using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Audio.Components;
using Content.Shared.GameTicking;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Audio;

/// <summary>
/// Chooses an environmental reverb preset for a positional audio source and manages a per preset auxiliary slot cache.
/// All work happens on the client because the networked auxiliary field gets overwritten by state sync.
/// </summary>
public abstract partial class SharedDynamicAudioSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Global volume adjustments
    public const float SpaceVolumePenaltyDb = -10f;

    // Default environmental presets
    private static readonly ProtoId<AudioPresetPrototype> DefaultPreset = "LivingRoom";
    private static readonly ProtoId<AudioPresetPrototype> SpacePreset = "InSpace";
    private static readonly ProtoId<AudioPresetPrototype> PlanetPreset = "Forest";

    // Room size presets based on tile count limits
    private static readonly (int MaxTiles, ProtoId<AudioPresetPrototype> Preset)[] AreaPresets =
    [
        (10, "Underwater"),
        (30, "Bathroom"),
        (70, "SpaceStationSmallRoom"),
        (120, "SpaceStationMediumRoom"),
        (150, "SpaceStationLargeRoom"),
        (200, "SpaceStationHall"),
    ];

    // Room scanning and pathfinding constraints
    private const int MaxRoomScanRadius = 8;
    private const int MaxRoomTileCount = 200;
    private static readonly Vector2i[] Directions =
    [
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
    ];

    // Cache limits to prevent OpenAL slot overflow
    private const int MaxCachedAuxiliaries = 48;

    // Runtime tracking and cache
    private readonly Dictionary<ProtoId<AudioPresetPrototype>, EntityUid> _auxiliaryCache = new();
    private readonly Dictionary<ProtoId<AudioPresetPrototype>, TimeSpan> _auxiliaryLastUsed = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ClearAllAuxiliaries();
    }

    /// <summary>
    /// Clears the auxiliary slot cache when the round restarts.
    /// </summary>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        ClearAllAuxiliaries();
    }

    private void ClearAllAuxiliaries()
    {
        foreach (var auxUid in _auxiliaryCache.Values)
        {
            if (!Exists(auxUid))
                continue;

            if (TryComp<AudioAuxiliaryComponent>(auxUid, out var auxComp))
            {
                auxComp.Auxiliary?.SetEffect(null);
                auxComp.Auxiliary?.Dispose();
            }

            // Delete the attached effect entity before deleting the auxiliary slot itself
            if (TryComp<AudioAuxiliaryComponent>(auxUid, out var auxComp2) && auxComp2.Effect is { } effectUid)
            {
                if (Exists(effectUid))
                    QueueDel(effectUid);
            }

            QueueDel(auxUid);
        }

        _auxiliaryCache.Clear();
        _auxiliaryLastUsed.Clear();
    }

    /// <summary>
    /// Returns the auxiliary slot for this audio entity surroundings.
    /// Creates one lazily per preset and caches it so all audio in the same room reuses the same slot.
    /// </summary>
    public EntityUid? GetOrCreateAuxiliaryFor(Entity<AudioComponent> audio, EntityUid? source = null)
    {
        var presetId = GetAreaPreset(audio, source, _playerManager.LocalEntity);

        if (_auxiliaryCache.TryGetValue(presetId, out var cached) && Exists(cached))
        {
            _auxiliaryLastUsed[presetId] = _timing.CurTime;
            return cached;
        }

        // Prevent OpenAL limit overflow by evicting the oldest slot if the cache is full
        if (_auxiliaryCache.Count >= MaxCachedAuxiliaries)
            EvictOldestAuxiliary();

        if (!TryCreateAuxiliary(presetId, out var auxUid))
            return null;

        _auxiliaryLastUsed[presetId] = _timing.CurTime;
        return auxUid;
    }

    /// <summary>
    /// Removes the least recently used auxiliary slot and its attached effect to free up memory.
    /// </summary>
    private void EvictOldestAuxiliary()
    {
        ProtoId<AudioPresetPrototype> oldestPreset = default;
        var oldestTime = TimeSpan.MaxValue;

        foreach (var (preset, lastUsed) in _auxiliaryLastUsed)
        {
            if (lastUsed < oldestTime)
            {
                oldestTime = lastUsed;
                oldestPreset = preset;
            }
        }

        if (oldestPreset == default)
            return;

        if (_auxiliaryCache.Remove(oldestPreset, out var oldUid))
        {
            if (Exists(oldUid))
            {
                if (TryComp<AudioAuxiliaryComponent>(oldUid, out var auxComp))
                {
                    // Explicitly detach the EFX effect and dispose the OpenAL auxiliary slot handle
                    auxComp.Auxiliary?.SetEffect(null);
                    auxComp.Auxiliary?.Dispose();

                    // Delete the attached effect entity before deleting the auxiliary slot itself
                    if (auxComp.Effect is { } effectUid && Exists(effectUid))
                        QueueDel(effectUid);
                }

                QueueDel(oldUid);
            }
            _auxiliaryLastUsed.Remove(oldestPreset);
        }
    }

    /// <summary>
    /// Determines the best reverb preset based on vacuum state and room size.
    /// </summary>
    private ProtoId<AudioPresetPrototype> GetAreaPreset(
        Entity<AudioComponent> audio,
        EntityUid? source,
        EntityUid? fallback)
    {
        var transform = ResolveSoundOrigin(audio, source, fallback);
        if (IsInBarotrauma(audio.Owner, transform, fallback))
            return SpacePreset;

        if (HasComp<BiomeComponent>(transform.GridUid))
            return PlanetPreset;

        if (!TryComp<MapGridComponent>(transform.GridUid, out var mapGrid))
            return DefaultPreset;

        var startTile = _map.TileIndicesFor(transform.GridUid.Value, mapGrid, _xform.GetMapCoordinates(transform));
        var area = CountTilesInRoom(startTile, (transform.GridUid.Value, mapGrid));

        foreach (var (maxTiles, preset) in AreaPresets)
        {
            if (area <= maxTiles)
                return preset;
        }

        return DefaultPreset;
    }

    /// <summary>
    /// Resolves the correct transform to base the reverb preset on.
    /// </summary>
    private TransformComponent ResolveSoundOrigin(
        Entity<AudioComponent> audio,
        EntityUid? source,
        EntityUid? fallback)
    {
        if (source is { Valid: true } sourceUid)
            return Transform(sourceUid);

        var transform = Transform(audio);
        if (_xform.GetWorldPosition(transform) == Vector2.Zero && fallback is { Valid: true } fb)
            return Transform(fb);

        return transform;
    }

    /// <summary>
    /// Checks if the sound or the listener is in a vacuum.
    /// </summary>
    private bool IsInBarotrauma(
        EntityUid audioUid,
        TransformComponent transform,
        EntityUid? fallback)
    {
        if (HasComp<InBarotraumaAudioEffectComponent>(audioUid))
            return true;

        if (fallback is { Valid: true } fb && HasComp<InBarotraumaAudioEffectComponent>(fb))
            return true;

        if (transform.GridUid is null)
            return true;

        return false;
    }

    /// <summary>
    /// Performs a flood fill to estimate the size of the room.
    /// </summary>
    private int CountTilesInRoom(Vector2i startTile, Entity<MapGridComponent> grid)
    {
        var visited = new HashSet<Vector2i> { startTile };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(startTile);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (Math.Abs(current.X - startTile.X) + Math.Abs(current.Y - startTile.Y) > MaxRoomScanRadius)
                continue;

            if (visited.Count > MaxRoomTileCount)
                break;

            foreach (var direction in Directions)
            {
                var neighbour = current + direction;

                if (visited.Contains(neighbour) || !IsValidTile(neighbour, grid))
                    continue;

                visited.Add(neighbour);
                queue.Enqueue(neighbour);
            }
        }

        return visited.Count;
    }

    /// <summary>
    /// Checks if a tile is free of solid walls.
    /// </summary>
    private bool IsValidTile(Vector2i tilePos, Entity<MapGridComponent> grid)
    {
        if (!_map.TryGetTileRef(grid.Owner, grid.Comp, tilePos, out _))
            return false;

        var worldPos = _map.GridTileToWorld(grid.Owner, grid.Comp, tilePos);
        foreach (var ent in _lookup.GetEntitiesInRange(worldPos, 0.01f))
        {
            if (!TryComp<PhysicsComponent>(ent, out var phys)
                || phys.BodyType != BodyType.Static
                || !phys.Hard)
                continue;

            const int blockedLayers = (int)(CollisionGroup.Impassable | CollisionGroup.HighImpassable);
            if ((phys.CollisionLayer & blockedLayers) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates and caches a new auxiliary effect slot for a specific preset.
    /// </summary>
    private bool TryCreateAuxiliary(
        ProtoId<AudioPresetPrototype> presetId,
        [NotNullWhen(true)] out EntityUid auxUid)
    {
        auxUid = default;

        if (!_prototype.TryIndex(presetId, out var preset))
            return false;

        var effect = _audio.CreateEffect();
        var aux = _audio.CreateAuxiliary();

        _audio.SetEffectPreset(effect.Entity, effect.Component, preset);
        _audio.SetEffect(aux.Entity, aux.Component, effect.Entity);

        if (!Exists(aux.Entity))
            return false;

        auxUid = aux.Entity;
        _auxiliaryCache[presetId] = auxUid;
        return true;
    }
}
