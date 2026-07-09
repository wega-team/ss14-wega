using System.Numerics;
using Content.Shared.Administration.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class ResurrectionCrystalSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResurrectionCrystalComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ResurrectionCrystalComponent, ResurrectionCrystalAction>(OnDoAfter);

        SubscribeLocalEvent<ResurrectionCrystalAffectedComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnUseInHand(Entity<ResurrectionCrystalComponent> ent, ref UseInHandEvent args)
    {
        var ev = new ResurrectionCrystalAction();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.Duration, ev, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        });
    }

    private void OnDoAfter(Entity<ResurrectionCrystalComponent> ent, ref ResurrectionCrystalAction args)
    {
        if (args.Cancelled)
            return;

        EnsureComp<ResurrectionCrystalAffectedComponent>(args.User);
        _audio.PlayPredicted(ent.Comp.UseSound, Transform(ent).Coordinates, null);
        _popup.PopupEntity(Loc.GetString($"resurrection-crystal-effect"), args.User, args.User);
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<ResurrectionCrystalAffectedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var targetCoords = FindRandomSafeBeacon(ent.Owner, ent.Comp.MinDistance);
        if (targetCoords == null)
            return;

        _rejuvenate.PerformRejuvenate(ent);

        _popup.PopupPredicted(Loc.GetString("resurrection-crystal-revive-effect", ("name", Identity.Name(ent.Owner, EntityManager))),
            ent.Owner, null, PopupType.LargeCaution);
        _transform.SetCoordinates(ent.Owner, targetCoords.Value);

        RemComp<ResurrectionCrystalAffectedComponent>(ent);
    }

    private EntityCoordinates? FindRandomSafeBeacon(EntityUid player, float minDistance)
    {
        var playerTransform = Transform(player);
        var playerPos = playerTransform.Coordinates.Position;

        var beacons = new List<EntityUid>();
        var beaconQuery = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out _, out var xform))
        {
            if (xform.MapID != playerTransform.MapID)
                continue;

            if (HasComp<MegafaunaComponent>(beaconUid) || HasComp<MobStateComponent>(beaconUid))
                continue;

            var beaconPos = xform.Coordinates.Position;
            var distance = Vector2.Distance(playerPos, beaconPos);
            if (distance < minDistance)
                continue;

            beacons.Add(beaconUid);
        }

        if (beacons.Count == 0)
            return null;

        var selectedBeacon = _random.Pick(beacons);
        var beaconCoords = Transform(selectedBeacon).Coordinates;

        return beaconCoords;
    }
}
