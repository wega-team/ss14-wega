using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// Attempts to shoot directly at a target entity without requiring aim coordinates.
    /// </summary>
    public bool AttemptDirectShoot(EntityUid user, EntityUid gunUid, EntityUid target, GunComponent? gun = null)
    {
        if (!Resolve(gunUid, ref gun, false))
            return false;

        var fromCoordinates = Transform(user).Coordinates;

        // Check if we can shoot
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = (gunUid, gun)
        };
        RaiseLocalEvent(gunUid, ref prevention);
        if (prevention.Cancelled)
            return false;

        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return false;

        // Get ammo
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, user);
        RaiseLocalEvent(gunUid, ev);

        if (ev.Ammo.Count <= 0)
        {
            // Empty gun
            var emptyGunShotEvent = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gunUid, ref emptyGunShotEvent);

            Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
            return false;
        }

        // Shoot directly at the target
        ShootDirect(gunUid, gun, target, ev.Ammo, user);
        UpdateAmmoCount(gunUid);

        var shotEv = new GunShotEvent(user, ev.Ammo);
        RaiseLocalEvent(gunUid, ref shotEv);

        return true;
    }

    /// <summary>
    /// Shoots directly at a target entity without requiring aim coordinates.
    /// Used for executions and point-blank shots.
    /// </summary>
    protected virtual bool ShootDirect(EntityUid gunUid, GunComponent gun, EntityUid target, List<(EntityUid? Entity, IShootable Shootable)> ammo, EntityUid user)
    {
        var fromCoordinates = Transform(user).Coordinates;
        var toCoordinates = Transform(target).Coordinates;

        Shoot((gunUid, gun), ammo, fromCoordinates, toCoordinates, out _, user);
        return true;
    }

    /// <summary>
    /// Attempts to shoot at the target coordinates, ignoring fire rate cooldown.
    /// Useful for boss attacks and scripted events.
    /// </summary>
    public bool ForceShoot(EntityUid user, EntityUid gunUid, GunComponent gun, EntityCoordinates toCoordinates, EntityUid? target = null)
    {
        gun.ShootCoordinates = toCoordinates;
        gun.Target = target;
        var result = ForceShootInternal(user, gunUid, gun);
        gun.ShotCounter = 0;
        DirtyField(gunUid, gun, nameof(GunComponent.ShotCounter));
        return result;
    }

    /// <summary>
    /// Force shoots by assuming the gun is the user at default coordinates, ignoring fire rate cooldown.
    /// </summary>
    public bool ForceShoot(EntityUid gunUid, GunComponent gun)
    {
        var coordinates = new EntityCoordinates(gunUid, gun.DefaultDirection);
        gun.ShootCoordinates = coordinates;
        var result = ForceShootInternal(gunUid, gunUid, gun);
        gun.ShotCounter = 0;
        return result;
    }

    /// <summary>
    /// Force shoots directly at a target entity without requiring aim coordinates and ignoring fire rate cooldown.
    /// </summary>
    public bool ForceDirectShoot(EntityUid user, EntityUid gunUid, EntityUid target, GunComponent? gun = null)
    {
        if (!Resolve(gunUid, ref gun, false))
            return false;

        var fromCoordinates = Transform(user).Coordinates;

        // Check if we can shoot
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = (gunUid, gun)
        };
        RaiseLocalEvent(gunUid, ref prevention);
        if (prevention.Cancelled)
            return false;

        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return false;

        // Get ammo
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, user);
        RaiseLocalEvent(gunUid, ev);

        if (ev.Ammo.Count <= 0)
        {
            // Empty gun
            var emptyGunShotEvent = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gunUid, ref emptyGunShotEvent);

            Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
            return false;
        }

        // Shoot directly at the target
        ForceShootDirect(gunUid, gun, target, ev.Ammo, user);
        UpdateAmmoCount(gunUid);

        var shotEv = new GunShotEvent(user, ev.Ammo);
        RaiseLocalEvent(gunUid, ref shotEv);

        return true;
    }

    private bool ForceShootInternal(EntityUid user, EntityUid gunUid, GunComponent gun)
    {
        if (gun.FireRateModified <= 0f)
            return false;

        var toCoordinates = gun.ShootCoordinates;

        if (toCoordinates == null)
            return false;

        // check if anything wants to prevent shooting
        var prevention = new ShotAttemptedEvent
        {
            User = user,
            Used = (gunUid, gun)
        };
        RaiseLocalEvent(gunUid, ref prevention);
        if (prevention.Cancelled)
            return false;

        RaiseLocalEvent(user, ref prevention);
        if (prevention.Cancelled)
            return false;

        // For force shooting, we always do 1 shot
        var shots = 1;

        var attemptEv = new AttemptShootEvent(user, null);
        RaiseLocalEvent(gunUid, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            if (attemptEv.Message != null)
            {
                PopupSystem.PopupClient(attemptEv.Message, gunUid, user);
            }
            gun.BurstActivated = false;
            gun.BurstShotsCount = 0;
            return false;
        }

        var fromCoordinates = Transform(user).Coordinates;
        // Remove ammo
        var ev = new TakeAmmoEvent(shots, new List<(EntityUid? Entity, IShootable Shootable)>(), fromCoordinates, user);

        if (shots > 0)
            RaiseLocalEvent(gunUid, ev);

        DebugTools.Assert(ev.Ammo.Count <= shots);
        DebugTools.Assert(shots >= 0);
        UpdateAmmoCount(gunUid);

        if (ev.Ammo.Count <= 0)
        {
            // triggers effects on the gun if it's empty
            var emptyGunShotEvent = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gunUid, ref emptyGunShotEvent);

            gun.BurstActivated = false;
            gun.BurstShotsCount = 0;

            // Play empty gun sounds if relevant
            if (shots > 0)
            {
                PopupSystem.PopupCursor(ev.Reason ?? Loc.GetString("gun-magazine-fired-empty"));
                Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                return false;
            }

            return false;
        }

        // Shoot confirmed
        Shoot((gunUid, gun), ev.Ammo, fromCoordinates, toCoordinates.Value, out var userImpulse);
        var shotEv = new GunShotEvent(user, ev.Ammo);
        RaiseLocalEvent(gunUid, ref shotEv);

        if (!userImpulse || !TryComp<PhysicsComponent>(user, out var userPhysics))
            return true;

        var shooterEv = new ShooterImpulseEvent();
        RaiseLocalEvent(user, ref shooterEv);

        if (shooterEv.Push)
            CauseImpulse(fromCoordinates, toCoordinates.Value, (user, userPhysics));
        return true;
    }

    /// <summary>
    /// Force shoots directly at a target entity without requiring aim coordinates.
    /// Used for boss attacks and scripted events.
    /// </summary>
    protected virtual bool ForceShootDirect(EntityUid gunUid, GunComponent gun, EntityUid target, List<(EntityUid? Entity, IShootable Shootable)> ammo, EntityUid user)
    {
        var fromCoordinates = Transform(user).Coordinates;
        var toCoordinates = Transform(target).Coordinates;

        Shoot((gunUid, gun), ammo, fromCoordinates, toCoordinates, out _, user);
        return true;
    }
}
