using System.Numerics;
using Content.Shared.Pinpointer;
using Robust.Shared.Map.Components;

namespace Content.Server.Pinpointer;

public sealed partial class NavMapSystem
{
    public void RefreshGridWithOffset(EntityUid uid, NavMapComponent component, MapGridComponent mapGrid, Vector2 worldOffset)
    {
        // Clear stale data
        component.Chunks.Clear();
        component.Beacons.Clear();

        var query = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var qUid, out var qNavComp, out var qTransComp))
        {
            if (qTransComp.ParentUid != uid)
                continue;

            UpdateNavMapBeaconData(qUid, qNavComp);
        }

        var tileSize = mapGrid.TileSize;
        var offsetInTiles = new Vector2i(
            (int)(worldOffset.X / tileSize),
            (int)(worldOffset.Y / tileSize)
        );

        // Loop over all tiles
        var tileRefs = _mapSystem.GetAllTiles(uid, mapGrid);

        foreach (var tileRef in tileRefs)
        {
            var originalTile = tileRef.GridIndices;
            var finalTile = originalTile + offsetInTiles;

            var chunkOrigin = SharedMapSystem.GetChunkIndices(finalTile, ChunkSize);
            var chunk = EnsureChunk(component, chunkOrigin);
            chunk.LastUpdate = _gameTiming.CurTick;

            RefreshTileEntityContentsWithOffset(component, mapGrid, chunkOrigin, originalTile, finalTile, setFloor: true);
        }

        Dirty(uid, component);
    }

    private void RefreshTileEntityContentsWithOffset(
        NavMapComponent component,
        MapGridComponent mapGrid,
        Vector2i offsetChunkOrigin,
        Vector2i originalTile,
        Vector2i finalTile,
        bool setFloor)
    {
        var relative = SharedMapSystem.GetChunkRelative(finalTile, ChunkSize);
        var chunk = EnsureChunk(component, offsetChunkOrigin);
        ref var tileData = ref chunk.TileData[GetTileIndex(relative)];

        // Clear all data except for floor bits
        if (setFloor)
            tileData = FloorMask;
        else
            tileData &= FloorMask;
#pragma warning disable CS0618 // It drives me crazy
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(mapGrid.Owner, mapGrid, originalTile);
#pragma warning restore CS0618
        while (enumerator.MoveNext(out var ent))
        {
            if (!_airtightQuery.TryComp(ent, out var airtight))
                continue;

            var category = GetEntityType(ent.Value);
            if (category == NavMapChunkType.Invalid)
                continue;

            var directions = (int)airtight.AirBlockedDirection;
            tileData |= directions << (int)category;
        }

        // Remove walls that intersect with doors
        var shiftedAirlockBits = (tileData & AirlockMask) >> ((int)NavMapChunkType.Airlock - (int)NavMapChunkType.Wall);
        tileData &= ~shiftedAirlockBits;
    }
}
