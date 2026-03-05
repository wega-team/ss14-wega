using System.Linq;
using Content.Shared.GPS;
using Content.Shared.GPS.Components;
using Content.Shared.Light.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.GPS.Systems;

public sealed partial class HandheldGpsUiSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private TimeSpan _lastUpdate = TimeSpan.Zero;
    private const float UpdateInterval = 1.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGpsUiComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<HandheldGpsUiComponent, UpdateGpsNameMessage>(OnUpdateName);
        SubscribeLocalEvent<HandheldGpsUiComponent, UpdateGpsDescriptionMessage>(OnUpdateDescription);
        SubscribeLocalEvent<HandheldGpsUiComponent, ToggleGpsBroadcastMessage>(OnToggleBroadcast);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;
        if (currentTime - _lastUpdate < TimeSpan.FromSeconds(UpdateInterval))
            return;

        _lastUpdate = currentTime;

        var query = EntityQueryEnumerator<HandheldGpsUiComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, GpsUiKey.Key))
                UpdateUi(uid, comp);
        }
    }

    private void OnUIOpened(Entity<HandheldGpsUiComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent.Owner, ent.Comp);
    }

    private void OnUpdateName(EntityUid uid, HandheldGpsUiComponent component, UpdateGpsNameMessage msg)
    {
        if (TryComp<HandheldGPSComponent>(uid, out var gps))
        {
            gps.GPSName = msg.NewName.Trim();
            Dirty(uid, gps);
            UpdateUi(uid, component);
        }
    }

    private void OnUpdateDescription(EntityUid uid, HandheldGpsUiComponent component, UpdateGpsDescriptionMessage msg)
    {
        if (TryComp<HandheldGPSComponent>(uid, out var gps))
        {
            gps.GPSDesc = msg.NewDescription.Trim();
            Dirty(uid, gps);
            UpdateUi(uid, component);
        }
    }

    private void OnToggleBroadcast(EntityUid uid, HandheldGpsUiComponent component, ToggleGpsBroadcastMessage msg)
    {
        component.BroadcastEnabled = msg.Enabled;
        Dirty(uid, component);
        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, HandheldGpsUiComponent component)
    {
        if (!_ui.HasUi(uid, GpsUiKey.Key))
            return;

        var transform = Transform(uid);
        var mapCoords = _transform.GetMapCoordinates(uid, xform: transform);
        var currentCoords = ((int)mapCoords.X, (int)mapCoords.Y);

        string gpsName = string.Empty;
        string gpsDesc = string.Empty;
        if (TryComp<HandheldGPSComponent>(uid, out var gps))
        {
            gpsName = gps.GPSName;
            gpsDesc = gps.GPSDesc;
        }

        var mapUid = GetNetEntity(Transform(uid).GridUid);
        var otherGpsList = GetOtherGpsDevices(uid, mapCoords.MapId);
        var navBeacons = GetNavBeacons(uid, mapCoords.MapId);
        var lavaTiles = GetLavaTiles(uid);

        var state = new GpsUpdateState(
            mapUid,
            gpsName,
            gpsDesc,
            component.BroadcastEnabled,
            currentCoords,
            otherGpsList,
            navBeacons,
            lavaTiles
        );

        _ui.SetUiState(uid, GpsUiKey.Key, state);
    }

    private List<GpsDeviceInfo> GetOtherGpsDevices(EntityUid currentGps, MapId mapId)
    {
        var result = new List<GpsDeviceInfo>();
        var currentPos = _transform.GetMapCoordinates(currentGps);

        var query = EntityQueryEnumerator<HandheldGPSComponent>();
        while (query.MoveNext(out var uid, out var gps))
        {
            if (uid == currentGps)
                continue;

            var otherTransform = Transform(uid);
            if (otherTransform.MapID != mapId)
                continue;

            if (TryComp<HandheldGpsUiComponent>(uid, out var broadcast) && !broadcast.BroadcastEnabled)
                continue;

            var otherPos = _transform.GetMapCoordinates(uid, xform: otherTransform);
            var distance = (otherPos.Position - currentPos.Position).Length();

            result.Add(new GpsDeviceInfo(
                GetNetEntity(uid),
                gps.GPSName,
                ((int)otherPos.X, (int)otherPos.Y),
                distance,
                Loc.GetString(gps.GPSDesc)
            ));
        }

        return result.OrderBy(g => g.Distance).ToList();
    }

    private List<NavBeaconInfo> GetNavBeacons(EntityUid currentGps, MapId mapId)
    {
        var result = new List<NavBeaconInfo>();
        var currentPos = _transform.GetMapCoordinates(currentGps);

        var query = EntityQueryEnumerator<NavMapBeaconComponent>();
        while (query.MoveNext(out var uid, out var beacon))
        {
            if (!beacon.Enabled)
                continue;

            var transform = Transform(uid);
            if (transform.MapID != mapId)
                continue;

            var beaconPos = _transform.GetMapCoordinates(uid, xform: transform);
            var distance = (beaconPos.Position - currentPos.Position).Length();

            var beaconName = MetaData(uid).EntityName;
            if (!string.IsNullOrEmpty(beacon.Text))
                beaconName = Loc.GetString(beacon.Text);
            else if (!string.IsNullOrEmpty(beacon.DefaultText))
                beaconName = Loc.GetString(beacon.DefaultText);

            result.Add(new NavBeaconInfo(
                beaconName,
                Loc.GetString(beacon.DefaultDesc),
                ((int)beaconPos.X, (int)beaconPos.Y),
                distance,
                beacon.Color,
                beacon.Enabled
            ));
        }

        return result.OrderBy(b => b.Distance).ToList();
    }

    private List<LavaTileInfo> GetLavaTiles(EntityUid currentGps)
    {
        var result = new List<LavaTileInfo>();
        var currentPos = _transform.GetMapCoordinates(currentGps);

        var emissions = _lookupSystem.GetEntitiesInRange<TileEmissionComponent>(currentPos, 32f);
        foreach (var emission in emissions)
        {
            var lavaPos = _transform.GetMapCoordinates(emission);
            var distance = (lavaPos.Position - currentPos.Position).Length();

            result.Add(new LavaTileInfo(
                ((int)lavaPos.X, (int)lavaPos.Y),
                emission.Comp.Color,
                distance
            ));
        }

        return result.OrderBy(l => l.Distance).ToList();
    }
}
