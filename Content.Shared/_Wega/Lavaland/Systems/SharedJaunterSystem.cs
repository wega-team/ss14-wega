using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lavaland.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Shuttles.Components;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Shared.Jaunter;

public sealed partial class SharedJaunterSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId Lava = "FloorLavaEntity";
    private const float PROBCHANCE = 0.05f;

    public bool TryFindJaunter(EntityUid user, [NotNullWhen(true)] out EntityUid? jaunter)
    {
        jaunter = null;
        var heldItems = _hands.EnumerateHeld(user);
        foreach (var item in heldItems)
        {
            if (HasComp<JaunterComponent>(item))
            {
                jaunter = item;
                return true;
            }
        }

        if (TryComp<InventoryComponent>(user, out var inventory))
        {
            var enumerator = _inventory.GetSlotEnumerator((user, inventory));
            while (enumerator.NextItem(out var item))
            {
                if (HasComp<JaunterComponent>(item))
                {
                    jaunter = item;
                    return true;
                }

                if (TryFindJaunterInStorage(item, out var storageJaunter))
                {
                    jaunter = storageJaunter;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindJaunterInStorage(EntityUid storageEntity, [NotNullWhen(true)] out EntityUid? jaunter)
    {
        jaunter = null;
        if (!TryComp<StorageComponent>(storageEntity, out var storage))
            return false;

        foreach (var itemTuple in storage.StoredItems)
        {
            var item = itemTuple.Key;
            if (TryComp<JaunterComponent>(item, out _))
            {
                jaunter = item;
                return true;
            }

            if (TryFindJaunterInStorage(item, out var nestedJaunter))
            {
                jaunter = nestedJaunter;
                return true;
            }
        }

        return false;
    }

    public bool TryUseJaunter(EntityUid tripper, EntityUid jaunter)
    {
        bool moved = false;
        if (_random.Prob(PROBCHANCE)) // The first attempt at bad luck
        {
            var lavaEnts = new HashSet<EntityUid>();
            var lava = EntityQueryEnumerator<StepTriggerComponent>();
            while (lava.MoveNext(out var uid, out _))
            {
                if (lavaEnts.Count >= 50)
                    break;

                var proto = MetaData(uid).EntityPrototype;
                if (proto != null && proto == Lava)
                    lavaEnts.Add(uid);
            }

            if (lavaEnts.Count > 0)
            {
                _transform.SetCoordinates(tripper, Transform(_random.Pick(lavaEnts)).Coordinates);
                moved = true;
            }
        }
        else if (_random.Prob(PROBCHANCE * 2) && !moved) // The second attempt at bad luck
        {
            EntityUid? map = null;
            var maps = EntityQueryEnumerator<FTLDestinationComponent>();
            while (maps.MoveNext(out var uid, out var ftl))
            {
                if (ftl.Whitelist != null)
                    continue;

                map = uid;
            }

            if (map != null)
            {
                var coords = new EntityCoordinates(map.Value, new Vector2(_random.NextFloat(300, 500), _random.NextFloat(300, 500)));
                _transform.SetCoordinates(tripper, coords);
                moved = true;
            }
        }

        if (!moved)
        {
            var lucky = _random.Prob(PROBCHANCE); // The third attempt at bad luck
            var beaconsEnts = new HashSet<EntityUid>();
            var beacons = EntityQueryEnumerator<NavMapBeaconComponent>();
            while (beacons.MoveNext(out var uid, out _))
            {
                if (beaconsEnts.Count > 50)
                    break;

                if (lucky)
                {
                    if (HasComp<MegafaunaComponent>(uid))
                        beaconsEnts.Add(uid);
                }
                else
                {
                    beaconsEnts.Add(uid);
                }
            }

            if (lucky && beaconsEnts.Count == 0)
            {
                beaconsEnts.Clear();
                while (beacons.MoveNext(out var uid, out _))
                {
                    if (beaconsEnts.Count > 50)
                        break;

                    beaconsEnts.Add(uid);
                }
            }

            if (beaconsEnts.Count == 0)
                return false;

            _transform.SetCoordinates(tripper, Transform(_random.Pick(beaconsEnts)).Coordinates);
        }

        Del(jaunter);
        return true;
    }
}
