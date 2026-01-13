using System.Linq;
using System.Numerics;
using Content.Client.Pinpointer;
using Content.Shared.Atmos;
using Content.Shared.GPS;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client._Wega.GPS.UI;

public sealed class GpsNavMapControl : Control
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private EntityUid? _mapUid;
    private List<GpsDeviceInfo> _gpsDevices = new();
    private List<NavBeaconInfo> _navBeacons = new();
    private Vector2 _currentPosition;

    private float _zoom = 1.0f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 4.0f;

    private readonly Color _wallColor = new(102, 217, 102);
    private readonly Color _floorColor = new(30, 67, 30, 180);
    private readonly Color _gpsColor = new(76, 201, 240);
    private readonly Color _currentPosColor = new(255, 71, 87);
    private readonly Color _gridColor = new(100, 100, 100, 50);

    private bool _showBeaconLabels = true;
    private float _updateTimer;
    private const float UpdateInterval = 2.0f;

    public event Action<float>? OnZoomChanged;
    public Action<Vector2>? OnMapClicked;

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathHelper.Clamp(value, MinZoom, MaxZoom);
            OnZoomChanged?.Invoke(_zoom);
        }
    }

    public bool ShowBeaconLabels
    {
        get => _showBeaconLabels;
        set
        {
            _showBeaconLabels = value;
        }
    }

    public GpsNavMapControl()
    {
        IoCManager.InjectDependencies(this);

        RectClipContent = true;
        HorizontalExpand = true;
        VerticalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public void UpdateData(
        EntityUid? mapUid,
        Vector2 currentPosition,
        List<GpsDeviceInfo> gpsDevices,
        List<NavBeaconInfo> navBeacons)
    {
        _mapUid = mapUid;
        _currentPosition = currentPosition;
        _gpsDevices = gpsDevices;
        _navBeacons = navBeacons;
    }

    public void CenterOnPosition(Vector2 position)
    {
        _currentPosition = position;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), new Color(45, 35, 71, 200));

        if (!_mapUid.HasValue || !_entityManager.TryGetComponent(_mapUid.Value, out NavMapComponent? navMap))
        {
            DrawNoMapMessage(handle, size);
            return;
        }

        var center = size / 2;

        DrawGrid(handle, center, size);
        DrawMapTiles(handle, navMap, center);
        DrawGpsDevices(handle, center);
        DrawNavBeacons(handle, center);
        DrawCurrentPosition(handle, center);
    }

    private void DrawNoMapMessage(DrawingHandleScreen handle, Vector2 size)
    {
        var font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 14);
        var text = Loc.GetString("gps-ui-no-map-data");
        var textSize = handle.GetDimensions(font, text, 1.0f);

        var position = new Vector2(size.X / 2 - textSize.X / 2, size.Y / 2 - textSize.Y / 2);
        handle.DrawString(font, position, text, Color.White);
    }

    private void DrawGrid(DrawingHandleScreen handle, Vector2 center, Vector2 size)
    {
        float gridSize = 20.0f * Zoom;
        var gridColor = _gridColor;

        for (float x = center.X % gridSize; x < size.X; x += gridSize)
        {
            handle.DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), gridColor);
        }

        for (float y = center.Y % gridSize; y < size.Y; y += gridSize)
        {
            handle.DrawLine(new Vector2(0, y), new Vector2(size.X, y), gridColor);
        }
    }

    private void DrawMapTiles(DrawingHandleScreen handle, NavMapComponent navMap, Vector2 center)
    {
        if (!_entityManager.TryGetComponent(_mapUid, out MapGridComponent? grid))
            return;

        var tileSize = grid.TileSize;
        var scale = 4.0f * Zoom;

        foreach (var (_, chunk) in navMap.Chunks)
        {
            for (int i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i];
                if ((SharedNavMapSystem.FloorMask & tileData) == 0)
                    continue;

                var relativeTile = SharedNavMapSystem.GetTileFromIndex(i);
                var tileWorldPos = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * tileSize;

                var screenPos = WorldToScreen(tileWorldPos, center, scale);

                var tileScreenSize = tileSize * scale;
                if (tileScreenSize > 1.0f)
                {
                    var rect = new UIBox2(
                        screenPos.X - tileScreenSize / 2,
                        screenPos.Y - tileScreenSize / 2,
                        screenPos.X + tileScreenSize / 2,
                        screenPos.Y + tileScreenSize / 2
                    );

                    handle.DrawRect(rect, _floorColor);
                }

                if (Zoom > 1.0f)
                {
                    DrawTileWalls(handle, tileData, screenPos, tileScreenSize);
                }
            }
        }
    }

    private void DrawTileWalls(DrawingHandleScreen handle, int tileData, Vector2 screenPos, float tileSize)
    {
        if (tileSize < 2.0f) return;

        var wallSize = Math.Max(1.0f, tileSize * 0.15f);

        if ((tileData & (1 << ((int)AtmosDirection.North + (int)NavMapChunkType.Wall))) != 0)
        {
            var wallRect = new UIBox2(
                screenPos.X - tileSize / 2,
                screenPos.Y - tileSize / 2,
                screenPos.X + tileSize / 2,
                screenPos.Y - tileSize / 2 + wallSize
            );
            handle.DrawRect(wallRect, _wallColor);
        }

        if ((tileData & (1 << ((int)AtmosDirection.South + (int)NavMapChunkType.Wall))) != 0)
        {
            var wallRect = new UIBox2(
                screenPos.X - tileSize / 2,
                screenPos.Y + tileSize / 2 - wallSize,
                screenPos.X + tileSize / 2,
                screenPos.Y + tileSize / 2
            );
            handle.DrawRect(wallRect, _wallColor);
        }

        if ((tileData & (1 << ((int)AtmosDirection.East + (int)NavMapChunkType.Wall))) != 0)
        {
            var wallRect = new UIBox2(
                screenPos.X + tileSize / 2 - wallSize,
                screenPos.Y - tileSize / 2,
                screenPos.X + tileSize / 2,
                screenPos.Y + tileSize / 2
            );
            handle.DrawRect(wallRect, _wallColor);
        }

        if ((tileData & (1 << ((int)AtmosDirection.West + (int)NavMapChunkType.Wall))) != 0)
        {
            var wallRect = new UIBox2(
                screenPos.X - tileSize / 2,
                screenPos.Y - tileSize / 2,
                screenPos.X - tileSize / 2 + wallSize,
                screenPos.Y + tileSize / 2
            );
            handle.DrawRect(wallRect, _wallColor);
        }
    }

    private void DrawGpsDevices(DrawingHandleScreen handle, Vector2 center)
    {
        if (_gpsDevices.Count == 0) return;

        var scale = 4.0f * Zoom;
        var markerSize = Math.Max(2.0f, 6.0f * Zoom);

        foreach (var device in _gpsDevices)
        {
            var screenPos = WorldToScreen(
                new Vector2(device.Coordinates.X, device.Coordinates.Y),
                center,
                scale
            );

            handle.DrawCircle(screenPos, markerSize, _gpsColor);

            handle.DrawCircle(screenPos, markerSize + 1, Color.White.WithAlpha(100));

            if (_showBeaconLabels && Zoom > 1.0f && !string.IsNullOrEmpty(device.Name))
            {
                DrawLabel(handle, screenPos, device.Name, markerSize, _gpsColor);
            }
        }
    }

    private void DrawNavBeacons(DrawingHandleScreen handle, Vector2 center)
    {
        var enabledBeacons = _navBeacons.Where(b => b.Enabled).ToList();
        if (enabledBeacons.Count == 0) return;

        var scale = 4.0f * Zoom;
        var markerSize = Math.Max(2.0f, 5.0f * Zoom);

        foreach (var beacon in enabledBeacons)
        {
            var screenPos = WorldToScreen(
                new Vector2(beacon.Coordinates.X, beacon.Coordinates.Y),
                center,
                scale
            );

            var beaconColor = beacon.Color;

            var rect = new UIBox2(
                screenPos.X - markerSize,
                screenPos.Y - markerSize,
                screenPos.X + markerSize,
                screenPos.Y + markerSize
            );
            handle.DrawRect(rect, beaconColor);

            handle.DrawRect(rect, Color.White.WithAlpha(100));

            if (_showBeaconLabels && !string.IsNullOrEmpty(beacon.Name))
            {
                DrawLabel(handle, screenPos, beacon.Name, markerSize, beaconColor);
            }
        }
    }

    private void DrawLabel(DrawingHandleScreen handle, Vector2 screenPos, string text, float markerSize, Color backgroundColor)
    {
        var fontSize = (int)Math.Clamp(10 * Zoom, 8, 14);
        var font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), fontSize);
        var textSize = handle.GetDimensions(font, text, 1.0f);

        var labelRect = new UIBox2(
            screenPos.X - textSize.X / 2 - 3,
            screenPos.Y + markerSize + 2,
            screenPos.X + textSize.X / 2 + 3,
            screenPos.Y + markerSize + textSize.Y + 5
        );

        handle.DrawRect(labelRect, backgroundColor.WithAlpha(200));

        handle.DrawString(
            font,
            new Vector2(screenPos.X - textSize.X / 2, screenPos.Y + markerSize + 2),
            text,
            Color.White
        );
    }

    private void DrawCurrentPosition(DrawingHandleScreen handle, Vector2 center)
    {
        var scale = 4.0f * Zoom;
        var markerSize = Math.Max(3.0f, 8.0f * Zoom);

        var screenPos = WorldToScreen(_currentPosition, center, scale);

        var points = new Vector2[]
        {
            new(screenPos.X, screenPos.Y - markerSize),
            new(screenPos.X - markerSize * 0.866f, screenPos.Y + markerSize * 0.5f),
            new(screenPos.X + markerSize * 0.866f, screenPos.Y + markerSize * 0.5f)
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, points, _currentPosColor);
        handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, points, Color.White);

        if (Zoom > 1.5f)
        {
            DrawLabel(handle, screenPos, Loc.GetString("gps-ui-you"), markerSize, _currentPosColor);
        }
    }

    private Vector2 WorldToScreen(Vector2 worldPos, Vector2 center, float scale)
    {
        var offset = worldPos - _currentPosition;
        var screenPos = center + offset * scale;

        screenPos.Y = center.Y - offset.Y * scale;

        return screenPos;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateTimer += args.DeltaSeconds;
        if (_updateTimer >= UpdateInterval)
            _updateTimer -= UpdateInterval;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        var zoomDelta = args.Delta.Y > 0 ? 0.1f : -0.1f;
        Zoom += zoomDelta;
    }
}
