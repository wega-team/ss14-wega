using System.Linq;
using Content.Shared._Wega.Ninja;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Station.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaTeleConsoleSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private const float PlayerNearbyRadius = 10f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<NinjaTeleConsoleComponent>(NinjaTeleConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<NinjaTeleConsoleConfirmMessage>(OnConfirm);
        });
    }

    private void OnUiOpened(EntityUid uid, NinjaTeleConsoleComponent comp, BoundUIOpenedEvent args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.Actor))
        {
            _popup.PopupEntity(Loc.GetString("ninja-teleconsole-unauthorized"), args.Actor, args.Actor);
            _ui.CloseUi(uid, NinjaTeleConsoleUiKey.Key, args.Actor);
            return;
        }

        UpdateUiState(uid);
    }

    private void OnConfirm(EntityUid uid, NinjaTeleConsoleComponent comp, NinjaTeleConsoleConfirmMessage args)
    {
        if (!HasComp<SpaceNinjaComponent>(args.Actor))
            return;

        var warp = PickSafeWarp();
        if (warp == null)
        {
            _popup.PopupEntity(Loc.GetString("ninja-teleconsole-no-warp"), args.Actor, args.Actor);
            return;
        }

        var warpCoords = Transform(warp.Value).Coordinates;
        _transform.SetCoordinates(args.Actor, warpCoords);

        _popup.PopupEntity(Loc.GetString("ninja-teleconsole-teleported"), args.Actor, args.Actor, PopupType.LargeCaution);
        _ui.CloseUi(uid, NinjaTeleConsoleUiKey.Key, args.Actor);
    }

    private void UpdateUiState(EntityUid uid)
    {
        var allWarps = GetStationWarps();
        var safeWarps = allWarps.Where(w => !HasPlayersNearby(w)).ToList();

        // Warn only if there are no safe warp points at all
        var noSafeWarp = allWarps.Count > 0 && safeWarps.Count == 0;
        _ui.SetUiState(uid, NinjaTeleConsoleUiKey.Key, new NinjaTeleConsoleBuiState(noSafeWarp, null));
    }

    /// <summary>
    /// Picks a random warp point with no players within <see cref="PlayerNearbyRadius"/> tiles.
    /// Falls back to any station warp if all are occupied.
    /// </summary>
    private EntityUid? PickSafeWarp()
    {
        var allWarps = GetStationWarps();
        if (allWarps.Count == 0)
            return null;

        var safeWarps = allWarps.Where(w => !HasPlayersNearby(w)).ToList();
        return safeWarps.Count > 0
            ? _random.Pick(safeWarps)
            : _random.Pick(allWarps);
    }

    private bool HasPlayersNearby(EntityUid warpUid)
    {
        var coords = _transform.GetMapCoordinates(warpUid);
        return _lookup.GetEntitiesInRange<ActorComponent>(coords, PlayerNearbyRadius).Count > 0;
    }

    private List<EntityUid> GetStationWarps()
    {
        var stationGrids = new HashSet<EntityUid>();
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out _, out var station))
        {
            foreach (var grid in station.Grids)
                stationGrids.Add(grid);
        }

        var warps = new List<EntityUid>();
        var warpQuery = EntityQueryEnumerator<WarpPointComponent>();
        while (warpQuery.MoveNext(out var warpUid, out var warp))
        {
            if (warp.Location == null)
                continue;
            var warpGrid = Transform(warpUid).GridUid;
            if (warpGrid == null || !stationGrids.Contains(warpGrid.Value))
                continue;
            warps.Add(warpUid);
        }

        return warps;
    }
}
