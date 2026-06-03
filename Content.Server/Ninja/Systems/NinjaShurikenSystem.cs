using System.Numerics;
using Content.Server.Ninja.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Makes ninja throwing stars fly through windows and grilles (GlassLayer entities without Opaque).
/// Also increases their throw speed, and handles the Shuriken Volley ability that fires a burst of
/// three shuriken projectiles one after another at a chosen location (the same projectile a ninja
/// borg's launcher fires).
/// </summary>
public sealed partial class NinjaShurikenSystem : EntitySystem
{
    private const float ShurikenThrowSpeed = 22f;

    // ── Shuriken Volley ability ────────────────────────────────────────────────
    private const string SuitDisableDelayId = "suit_powers";
    private const string ShurikenProjectile = "NinjaShurikenProjectile";
    private const float ShurikenAbilityCharge = 30f;
    private const float ShurikenProjectileSpeed = 25f;
    private const int ShurikenBurstCount = 3;
    // Delay between each shuriken of the burst, so they fire one after another rather than all at once.
    private static readonly TimeSpan ShurikenBurstInterval = TimeSpan.FromSeconds(0.15);

    private static readonly SoundSpecifier ShurikenThrowSound =
        new SoundPathSpecifier("/Audio/Weapons/star_hit.ogg");

    [Dependency] private NinjaCloakSystem _cloak = default!;
    [Dependency] private GunSystem _guns = default!;
    [Dependency] private SpaceNinjaSystem _ninja = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Pending shots for in-progress bursts, fired sequentially in Update.
    private readonly List<PendingShot> _pendingShots = new();

    private struct PendingShot
    {
        public EntityUid User;
        public Vector2 Direction;
        public TimeSpan FireAt;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaShurikenComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<SpaceNinjaComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaShurikenThrowEvent>(OnShurikenThrow);
    }

    private void OnPreventCollide(Entity<NinjaShurikenComponent> ent, ref PreventCollideEvent args)
    {
        var layer = args.OtherFixture.CollisionLayer;
        // Glass and grilles have GlassLayer bits (Impassable set) but NOT Opaque — walls have Opaque.
        var isGlass = (layer & (int) CollisionGroup.Impassable) != 0
                   && (layer & (int) CollisionGroup.Opaque)     == 0;
        if (isGlass)
            args.Cancelled = true;
    }

    private void OnBeforeThrow(Entity<SpaceNinjaComponent> ent, ref BeforeThrowEvent args)
    {
        if (HasComp<NinjaShurikenComponent>(args.ItemUid))
            args.ThrowSpeed = ShurikenThrowSpeed;
    }

    private void OnShurikenThrow(Entity<NinjaSuitComponent> ent, ref NinjaShurikenThrowEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        if (!_ninja.TryUseCharge(user, ShurikenAbilityCharge))
            return;

        var userMapPos = _transform.GetMapCoordinates(user);
        var targetMapPos = _transform.ToMapCoordinates(args.Target);
        var baseDir = targetMapPos.Position - userMapPos.Position;
        if (baseDir.LengthSquared() < 0.0001f)
            return;

        baseDir = baseDir.Normalized();

        // Fire the first shuriken immediately, then queue the rest to fire one after another.
        FireShuriken(user, baseDir);
        var now = _timing.CurTime;
        for (var i = 1; i < ShurikenBurstCount; i++)
        {
            _pendingShots.Add(new PendingShot
            {
                User = user,
                Direction = baseDir,
                FireAt = now + ShurikenBurstInterval * i,
            });
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingShots.Count == 0)
            return;

        var now = _timing.CurTime;
        for (var i = _pendingShots.Count - 1; i >= 0; i--)
        {
            var shot = _pendingShots[i];
            if (now < shot.FireAt)
                continue;

            if (Exists(shot.User))
                FireShuriken(shot.User, shot.Direction);

            _pendingShots.RemoveAt(i);
        }
    }

    private void FireShuriken(EntityUid user, Vector2 direction)
    {
        _audio.PlayPvs(ShurikenThrowSound, user);
        var shuriken = Spawn(ShurikenProjectile, Transform(user).Coordinates);
        _guns.ShootProjectile(shuriken, direction, Vector2.Zero, user, user, ShurikenProjectileSpeed);
    }
}
