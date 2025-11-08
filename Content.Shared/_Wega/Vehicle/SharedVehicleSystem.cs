using System.Numerics;
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    private static readonly ProtoId<TagPrototype> Dump = "DoorBumpOpener";
    private static readonly ProtoId<TagPrototype> Key = "VehicleKey";

    private const string KeySlot = "key_slot";

    public override void Initialize()
    {
        base.Initialize();
        InitializeRider();

        SubscribeLocalEvent<VehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<VehicleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonkAction);
        SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<VehicleComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);

        SubscribeLocalEvent<InVehicleComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
    }

    public override void Update(float frameTime)
    {
        var vehicleQuery = EntityQueryEnumerator<VehicleComponent, InputMoverComponent>();
        while (vehicleQuery.MoveNext(out var uid, out var vehicle, out var mover))
        {
            if (!vehicle.AutoAnimate)
                continue;

            // Why is this updating appearance data every tick, instead of when it needs to be updated???

            if (_mover.GetVelocityInput(mover).Sprinting == Vector2.Zero)
            {
                UpdateAutoAnimate(uid, false);
                continue;
            }

            UpdateAutoAnimate(uid, true);
        }
    }

    private void OnVehicleStartup(Entity<VehicleComponent> ent, ref ComponentStartup args)
    {
        UpdateDrawDepth(ent, 2);

        // This code should be purged anyway but with that being said this doesn't handle components being changed.
        if (TryComp<StrapComponent>(ent, out var strap))
        {
            ent.Comp.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffset = Vector2.Zero;
        }

        _modifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        if (ent.Comp.UseHand == true)
        {
            // Add a virtual item to rider's hand, unbuckle if we can't.
            if (!_virtualItemSystem.TrySpawnVirtualItemInHand(ent, args.Buckle))
            {
                _buckle.Unbuckle(args.Buckle.Owner, null);
                return;
            }
        }
        // Set up the rider and vehicle with each other
        EnsureComp<InputMoverComponent>(ent);
        EnsureComp<RiderComponent>(args.Buckle).Vehicle = ent;
        ent.Comp.Rider = args.Buckle.Owner;
        ent.Comp.LastRider = ent.Comp.Rider;
        Dirty(ent);

        Appearance.SetData(ent, VehicleVisuals.HideRider, true);

        _mover.SetRelay(args.Buckle.Owner, ent.Owner);

        // Update appearance stuff, add actions
        UpdateBuckleOffset(ent, Transform(ent));
        if (TryComp<InputMoverComponent>(ent, out var mover))
            UpdateDrawDepth(ent, GetDrawDepth(ent.Comp, Transform(ent), mover.RelativeRotation.Degrees));

        if (TryComp<UnpoweredFlashlightComponent>(ent, out var flashlight))
        {
            _actionsSystem.AddAction(args.Buckle, ref flashlight.ToggleActionEntity, flashlight.ToggleAction, ent);
        }

        if (ent.Comp.HornSound != null)
        {
            _actionsSystem.AddAction(args.Buckle, ref ent.Comp.HornActionEntity, ent.Comp.HornAction, ent);
        }

        _joints.ClearJoints(args.Buckle);

        _tagSystem.AddTag(ent, Dump);
    }

    private void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        // Clean up actions and virtual items
        _actionsSystem.RemoveProvidedActions(args.Buckle, ent);

        if (ent.Comp.UseHand == true)
            _virtualItemSystem.DeleteInHandsMatching(args.Buckle, ent);

        // Entity is no longer riding
        RemComp<RiderComponent>(args.Buckle);
        RemComp<RelayInputMoverComponent>(args.Buckle);
        _tagSystem.RemoveTag(ent, Dump);

        Appearance.SetData(ent, VehicleVisuals.HideRider, false);
        // Reset component
        ent.Comp.Rider = null;
        Dirty(ent);
    }

    private void OnHonkAction(Entity<VehicleComponent> vehicle, ref HonkActionEvent args)
    {
        if (args.Handled || vehicle.Comp.HornSound == null)
            return;

        // TODO: Need audio refactor maybe, just some way to null it when the stream is over.
        // For now better to just not loop to keep the code much cleaner.
        vehicle.Comp.HonkPlayingStream = _audioSystem.PlayPredicted(vehicle.Comp.HornSound, vehicle, vehicle)?.Entity;
        args.Handled = true;
    }

    private void OnEntInserted(Entity<VehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != KeySlot ||
            !_tagSystem.HasTag(args.Entity, Key))
            return;

        // Enable vehicle
        var inVehicle = EnsureComp<InVehicleComponent>(args.Entity);
        inVehicle.Vehicle = ent.Comp;

        ent.Comp.HasKey = true;

        var msg = Loc.GetString("vehicle-use-key",
            ("keys", args.Entity), ("vehicle", ent));
        if (_netManager.IsServer)
            _popupSystem.PopupEntity(msg, ent, args.OldParent, PopupType.Medium);

        // Audiovisual feedback
        _ambientSound.SetAmbience(ent, true);
        _modifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnEntRemoved(Entity<VehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != KeySlot || !RemComp<InVehicleComponent>(args.Entity))
            return;

        ent.Comp.HasKey = false;
        _ambientSound.SetAmbience(ent, false);
        _modifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<VehicleComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.HasKey)
        {
            args.ModifySpeed(0f, 0f);
        }
    }

    // TODO: Shitcode, needs to use sprites instead of actual offsets.
    private void OnMoveEvent(Entity<VehicleComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

        // This first check is just for safety
        if (ent.Comp.AutoAnimate && !HasComp<InputMoverComponent>(ent))
        {
            UpdateAutoAnimate(ent, false);
            return;
        }

        UpdateBuckleOffset(ent, args.Component);
        if (TryComp<InputMoverComponent>(ent, out var mover))
            UpdateDrawDepth(ent, GetDrawDepth(ent.Comp, args.Component, mover.RelativeRotation));
    }

    private void OnGettingPickedUpAttempt(Entity<InVehicleComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Vehicle == null || ent.Comp.Vehicle.Rider != null && ent.Comp.Vehicle.Rider != args.User)
            args.Cancel();
    }

    private int GetDrawDepth(VehicleComponent component, TransformComponent xform, Angle cameraAngle)
    {
        var itemDirection = cameraAngle.GetDir() switch
        {
            Direction.South => xform.LocalRotation.GetDir(),
            Direction.North => xform.LocalRotation.RotateDir(Direction.North),
            Direction.West => xform.LocalRotation.RotateDir(Direction.East),
            Direction.East => xform.LocalRotation.RotateDir(Direction.West),
            _ => Direction.South
        };

        return itemDirection switch
        {
            Direction.North => component.NorthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.South => component.SouthOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.West => component.WestOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            Direction.East => component.EastOver
                ? (int)DrawDepth.DrawDepth.Doors
                : (int)DrawDepth.DrawDepth.WallMountedItems,
            _ => (int)DrawDepth.DrawDepth.WallMountedItems
        };
    }

    private void UpdateBuckleOffset(Entity<VehicleComponent> ent, TransformComponent xform)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        // TODO: Strap should handle this but buckle E/C moment.
        var oldOffset = strap.BuckleOffset;

        strap.BuckleOffset = xform.LocalRotation.Degrees switch
        {
            < 45f => new(0, ent.Comp.SouthOverride),
            <= 135f => ent.Comp.BaseBuckleOffset,
            < 225f => new(0, ent.Comp.NorthOverride),
            <= 315f => new(ent.Comp.BaseBuckleOffset.X * -1, ent.Comp.BaseBuckleOffset.Y),
            _ => new(0, ent.Comp.SouthOverride)
        };

        if (!oldOffset.Equals(strap.BuckleOffset))
            Dirty(ent, strap);

        foreach (var buckledEntity in strap.BuckledEntities)
        {
            _transform.SetLocalPositionNoLerp(buckledEntity, strap.BuckleOffset);
        }
    }

    private void OnGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Rider == null)
            return;

        args.Entities.Add(ent.Comp.Rider.Value);
    }

    private void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        Appearance.SetData(uid, VehicleVisuals.DrawDepth, drawDepth);
    }

    private void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        Appearance.SetData(uid, VehicleVisuals.AutoAnimate, autoAnimate);
    }
}

/// <summary>
/// Stores the vehicle's draw depth mostly
/// </summary>
[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    /// <summary>
    /// What layer the vehicle should draw on (assumed integer)
    /// </summary>
    DrawDepth,
    /// <summary>
    /// Whether the wheels should be turning
    /// </summary>
    AutoAnimate,
    HideRider
}

/// <summary>
/// Raised when someone honks a vehicle horn
/// </summary>
public sealed partial class HonkActionEvent : InstantActionEvent
{
}
