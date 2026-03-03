using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed class ShipInBottleSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<TagPrototype> Swim = "CanSwim";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipInBottleComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<ShipInBottleComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryUseShipInBottle(args.User, ent);
    }

    public bool TryUseShipInBottle(EntityUid user, Entity<ShipInBottleComponent> bottle)
    {
        if (bottle.Comp.SpawnedBoat != null && Exists(bottle.Comp.SpawnedBoat))
        {
            return TryReturnBoat(user, bottle);
        }

        return TrySpawnBoat(user, bottle);
    }

    private bool TrySpawnBoat(EntityUid user, Entity<ShipInBottleComponent> bottle)
    {
        var userTransform = Transform(user);
        var userDirection = userTransform.LocalRotation.ToWorldVec().Normalized();
        var spawnPos = userTransform.Coordinates.Offset(userDirection * bottle.Comp.MaxSpawnDistance);

        if (!IsValidWaterTile(spawnPos))
        {
            _popup.PopupEntity(Loc.GetString("lavaland-artefacts-ship-in-bottle-not-water"), user, user);
            return false;
        }

        var boat = Spawn(bottle.Comp.BoatPrototype, spawnPos);
        bottle.Comp.SpawnedBoat = boat;

        _popup.PopupEntity(Loc.GetString("lavaland-artefacts-ship-in-bottle-spawned"), user, user);
        _appearance.SetData(bottle, VisualLayers.Enabled, true);
        return true;
    }

    private bool TryReturnBoat(EntityUid user, Entity<ShipInBottleComponent> bottle)
    {
        if (bottle.Comp.SpawnedBoat == null)
            return false;

        var boat = bottle.Comp.SpawnedBoat.Value;
        var userTransform = _transform.GetWorldPosition(user);
        var boatTransform = _transform.GetWorldPosition(boat);

        var distance = (userTransform - boatTransform).Length();
        if (distance > bottle.Comp.BoatCheckRadius)
        {
            _popup.PopupEntity(Loc.GetString("lavaland-artefacts-ship-in-bottle-too-far"), user, user);
            return false;
        }

        QueueDel(boat);
        bottle.Comp.SpawnedBoat = null;

        _popup.PopupEntity(Loc.GetString("lavaland-artefacts-ship-in-bottle-returned"), user, user);
        _appearance.SetData(bottle, VisualLayers.Enabled, false);
        return true;
    }

    private bool IsValidWaterTile(EntityCoordinates coordinates)
    {
        var entities = _lookup.GetEntitiesInRange<TagComponent>(coordinates, 0.1f);

        foreach (var entity in entities)
        {
            if (_tagSystem.HasTag(entity.Owner, Swim))
                return true;
        }

        return false;
    }
}
