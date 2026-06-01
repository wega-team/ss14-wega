using Content.Shared.Maps;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaEmergencyTeleportSystem : EntitySystem
{
    private const float EmergencyTeleportCharge = 150f;
    private const string SuitDisableDelayId = "suit_powers";

    private static readonly SoundSpecifier TeleportSound =
        new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [Dependency] private readonly NinjaCloakSystem _cloak = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmergencyTeleportEvent>(OnEmergencyTeleport);
    }

    private void OnEmergencyTeleport(Entity<NinjaSuitComponent> ent, ref NinjaEmergencyTeleportEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        if (!_ninja.TryUseCharge(user, EmergencyTeleportCharge))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        var coords = SelectRandomTileInRange(user, new Vector2(0.5f, 15f));
        if (coords != null)
        {
            _audio.PlayPvs(TeleportSound, user);
            _transform.SetCoordinates(user, coords.Value);
        }
    }

    private EntityCoordinates? SelectRandomTileInRange(EntityUid uid, Vector2 radius, int tries = 40)
    {
        var userCoords = Transform(uid).Coordinates;
        EntityCoordinates? targetCoords = null;

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return null;

        for (var i = 0; i < tries; i++)
        {
            var distance = (radius.Y - radius.X) * MathF.Sqrt(_random.NextFloat()) * (1 - (float) i / tries) + radius.X;
            var tempTargetCoords = userCoords.Offset(_random.NextAngle().ToVec() * distance);

            if (!_turf.TryGetTileRef(tempTargetCoords, out var tileRef)
                || _turf.IsSpace(tileRef.Value)
                || _turf.IsTileBlocked(tileRef.Value, (CollisionGroup) physics.CollisionMask))
                continue;

            targetCoords = tempTargetCoords;
            break;
        }

        return targetCoords;
    }
}
