using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Tag;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private MovementSpeedModifierSystem _modifier = default!;

    private static readonly ProtoId<TagPrototype> Swim = "CanSwim";
    private static readonly ProtoId<TagPrototype> Oar = "Oar";

    [SubscribeLocalEvent]
    private void OnBoatRefreshMovementSpeedModifiers(Entity<BoatComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        if (vehicle.Operator is not { } operatorUid ||
            !IsOnValidTile(ent.Owner, operatorUid, ent.Comp.RequiredOal))
        {
            args.ModifySpeed(0f, 0f);
        }
    }

    [SubscribeLocalEvent]
    private void OnBoatMove(Entity<BoatComponent> ent, ref MoveEvent args)
    {
        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        if (vehicle.Operator is not null)
        {
            _modifier.RefreshMovementSpeedModifiers(ent.Owner);
        }
    }

    [SubscribeLocalEvent]
    private void OnBoatOperatorSet(Entity<BoatComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (_vehicleQuery.TryComp(ent, out _))
        {
            _modifier.RefreshMovementSpeedModifiers(ent.Owner);
        }
    }

    /// <summary>
    /// Checks if the boat is on a valid tile (water) and has oar if required.
    /// </summary>
    private bool IsOnValidTile(EntityUid boat, EntityUid user, bool requiredOal)
    {
        var transform = Transform(boat);
        var coordinates = transform.Coordinates;

        var entities = _lookup.GetEntitiesInRange<TagComponent>(coordinates, 0.01f);
        foreach (var entity in entities)
        {
            if (_tagSystem.HasTag(entity.Owner, Swim))
            {
                if (!requiredOal)
                    return true;

                // Check if user has an oar in hand
                if (TryComp<HandsComponent>(user, out var hands))
                {
                    foreach (var heldItem in _hands.EnumerateHeld((user, hands)))
                    {
                        if (_tagSystem.HasTag(heldItem, Oar))
                            return true;
                    }
                }
            }
        }

        return false;
    }
}
