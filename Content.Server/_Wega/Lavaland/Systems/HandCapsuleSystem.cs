using Content.Server.Lavaland.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Components;
using Content.Shared.Popups;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.Lavaland.Systems;

public sealed partial class HandCapsuleSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandCapsuleComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<HandCapsuleComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        var userTransform = Transform(args.User);
        if (!HasComp<LavalandComponent>(userTransform.MapUid))
        {
            _popup.PopupEntity(Loc.GetString("lavaland-hand-capsule-spawn-failed"), args.User, args.User);
            return;
        }

        _loader.TryLoadGrid(userTransform.MapID, ent.Comp.CapsulePath, out _, offset: userTransform.Coordinates.Position);
        QueueDel(ent);
    }
}