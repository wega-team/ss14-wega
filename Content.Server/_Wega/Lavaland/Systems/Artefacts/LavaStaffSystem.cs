using Content.Server.Popups;
using Content.Server.Tiles;
using Content.Shared.Interaction;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Maps;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class LavaStaffSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LavaStaffComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<LavaStaffComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (TryInteractWithTile(ent, args))
        {
            args.Handled = _useDelay.TryResetDelay(ent.Owner);
            _audio.PlayPvs(ent.Comp.UseSound, ent);
        }
    }

    private bool TryInteractWithTile(Entity<LavaStaffComponent> ent, AfterInteractEvent args)
    {
        var comp = ent.Comp;
        var clickLocation = args.ClickLocation;

        if (!_mapManager.TryFindGridAt(_transform.ToMapCoordinates(clickLocation), out var gridUid, out var mapGrid))
            return false;

        var tileRef = _map.GetTileRef(gridUid, mapGrid, clickLocation);
        var tileDef = (ContentTileDefinition)_tileDefManager[tileRef.Tile.TypeId];

        if (args.Target != null && HasComp<TileEntityEffectComponent>(args.Target))
        {
            var proto = MetaData(args.Target.Value).EntityPrototype;
            if (proto == null)
                return false;

            if (proto != null && proto.ID == comp.LavaEntity)
            {
                QueueDel(args.Target.Value);

                _popup.PopupEntity(Loc.GetString("lava-staff-remove"), args.User, args.User);
                return true;
            }
        }

        if (args.Target == null && tileDef.ID == comp.BasaltTile)
        {
            var coordinates = _map.GridTileToLocal(gridUid, mapGrid, tileRef.GridIndices);
            Spawn(comp.LavaEntity, coordinates);

            _popup.PopupEntity(Loc.GetString("lava-staff-create"), args.User, args.User);
            return true;
        }

        _popup.PopupEntity(Loc.GetString("lava-staff-cant-use"), args.User, args.User);
        return false;
    }
}
