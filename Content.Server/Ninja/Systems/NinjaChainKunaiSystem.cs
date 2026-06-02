using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the Chain Kunai ability: fires a kunai projectile that pulls the victim to the ninja and knocks them down.
/// </summary>
public sealed partial class NinjaChainKunaiSystem : EntitySystem
{
    private const string SuitDisableDelayId = "suit_powers";
    private const float ChainKunaiCharge = 20f;
    private const float KunaiSpeed = 50f;

    // Placeholder — replace with a dedicated kunai throw sound when ready.
    private static readonly SoundSpecifier KunaiThrowSound =
        new SoundPathSpecifier("/Audio/_Wega/Weapons/Kunai/short-clang-of-metal-links.ogg");

    [Dependency] private NinjaCloakSystem _cloak = default!;
    [Dependency] private GunSystem _guns = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SpaceNinjaSystem _ninja = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, EntityUid> _kunaiNinja = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, ChainKunaiEvent>(OnChainKunai);
        SubscribeLocalEvent<ChainKunaiProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ChainKunaiProjectileComponent, ComponentShutdown>(OnKunaiShutdown);
    }

    private void OnChainKunai(Entity<NinjaSuitComponent> ent, ref ChainKunaiEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        if (!_ninja.TryUseCharge(user, ChainKunaiCharge))
            return;

        _audio.PlayPvs(KunaiThrowSound, user);

        var kunai = Spawn("ChainKunaiNinja", Transform(user).Coordinates);
        var kunaiComp = EnsureComp<ChainKunaiProjectileComponent>(kunai);
        kunaiComp.Ninja = user;
        _kunaiNinja[kunai] = user;
        Dirty(kunai, kunaiComp);

        var ninjaMapPos = _transform.GetMapCoordinates(user);
        var targetMapPos = _transform.ToMapCoordinates(args.Target);
        var direction = (targetMapPos.Position - ninjaMapPos.Position).Normalized();

        _guns.ShootProjectile(kunai, direction, Vector2.Zero, user, user, KunaiSpeed);
    }

    private void OnProjectileHit(Entity<ChainKunaiProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var victim = args.Target;

        if (!HasComp<MobStateComponent>(victim))
            return;

        if (!_kunaiNinja.TryGetValue(ent.Owner, out var ninja))
            ninja = ent.Comp.Ninja;

        if (!Exists(ninja) || ninja == victim)
            return;

        if (!TryComp<PhysicsComponent>(victim, out var physics))
            return;

        var ninjaPos = _transform.GetWorldPosition(ninja);
        var victimPos = _transform.GetWorldPosition(victim);
        var delta = ninjaPos - victimPos;

        if (delta == Vector2.Zero)
            return;

        _physics.SetLinearVelocity(victim, delta.Normalized() * ent.Comp.PullSpeed, body: physics);
        _stun.TryKnockdown(victim, ent.Comp.KnockdownDuration, true);
    }

    private void OnKunaiShutdown(EntityUid uid, ChainKunaiProjectileComponent comp, ComponentShutdown args)
    {
        _kunaiNinja.Remove(uid);
    }
}
