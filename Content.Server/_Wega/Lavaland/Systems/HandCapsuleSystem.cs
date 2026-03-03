using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.Lavaland.Systems;

public sealed partial class HandCapsuleSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandCapsuleComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<HandCapsuleComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        if (_useDelay.IsDelayed(ent.Owner))
            return;

        var userTransform = Transform(args.User);
        if (!HasComp<LavalandComponent>(userTransform.MapUid) || userTransform.MapUid != userTransform.GridUid)
        {
            _popup.PopupEntity(Loc.GetString("lavaland-hand-capsule-spawn-failed"), args.User, args.User);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        if (!_loader.TryLoadGrid(userTransform.MapID, ent.Comp.CapsulePath, out var grid, offset: userTransform.Coordinates.Position))
        {
            _popup.PopupEntity(Loc.GetString("lavaland-hand-capsule-spawn-failed"), args.User, args.User);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        var gridComp = grid.Value.Comp;
        var worldAABB = new Box2Rotated(
            gridComp.LocalAABB.Translated(userTransform.Coordinates.Position),
            Angle.Zero
        );

        var walls = _lookup.GetEntitiesIntersecting(userTransform.MapUid.Value, worldAABB, LookupFlags.Static);
        if (walls.Count > 0)
        {
            Del(grid); // I've checked, it won't delete you during the completion
            var boxSize = $"({gridComp.LocalAABB.Width:F1}x{gridComp.LocalAABB.Height:F1})";
            _popup.PopupEntity(Loc.GetString("lavaland-hand-capsule-spawn-failed-box", ("box", boxSize)), args.User, args.User);
            _useDelay.TryResetDelay(ent.Owner);
            return;
        }

        QueueDel(ent);
    }
}
