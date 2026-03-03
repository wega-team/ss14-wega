using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class LinkedCubeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LinkedCubeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LinkedCubeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LinkedCubeComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnMapInit(Entity<LinkedCubeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.IsPrimary && ent.Comp.LinkedCube == null)
        {
            var pair = Spawn(ent.Comp.PairPrototype, Transform(ent).Coordinates);
            ent.Comp.LinkedCube = pair;

            if (TryComp<LinkedCubeComponent>(pair, out var pairComp))
            {
                pairComp.LinkedCube = ent.Owner;
                pairComp.IsPrimary = false;
            }
        }
    }

    private void OnShutdown(Entity<LinkedCubeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.LinkedCube != null && Exists(ent.Comp.LinkedCube))
            QueueDel(ent.Comp.LinkedCube.Value);
    }

    private void OnUseInHand(Entity<LinkedCubeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryTeleportToLinkedCube(args.User, ent);
    }

    private bool TryTeleportToLinkedCube(EntityUid user, Entity<LinkedCubeComponent> cube)
    {
        if (cube.Comp.LinkedCube == null || !Exists(cube.Comp.LinkedCube))
        {
            _popup.PopupEntity(Loc.GetString("linked-cube-no-pair"), user, user);
            return false;
        }

        var linkedCube = cube.Comp.LinkedCube.Value;
        var cubeTransform = _transform.GetWorldPosition(Transform(cube));
        var linkedTransform = _transform.GetWorldPosition(Transform(linkedCube));

        var distance = (cubeTransform - linkedTransform).Length();
        if (distance < cube.Comp.MinTeleportDistance)
        {
            _popup.PopupEntity(Loc.GetString("linked-cube-too-close",
                ("distance", distance.ToString("F2"))), user, user);
            return false;
        }

        var mapUid = _transform.GetMap(linkedCube);
        if (mapUid == null)
            return false;

        _transform.SetCoordinates(user, new EntityCoordinates(mapUid.Value, linkedTransform));

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), user);
        _popup.PopupEntity(Loc.GetString("linked-cube-teleported"), user, user);

        return true;
    }
}
