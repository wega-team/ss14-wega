using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Lavaland.Events;
using Content.Server.Destructible;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class HeraldSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeraldComponent, ComponentShutdown>(OnMobStateChanged);

        SubscribeLocalEvent<HeraldComponent, HeraldTriShotActionEvent>(OnTriShot);
        SubscribeLocalEvent<HeraldComponent, HeraldSpreadShotActionEvent>(OnSpreadShot);
        SubscribeLocalEvent<HeraldComponent, HeraldTeleShotActionEvent>(OnTeleShot);
        SubscribeLocalEvent<HeraldComponent, HeraldMirrorActionEvent>(OnMirror);

        SubscribeLocalEvent<HeraldMirrorComponent, ComponentShutdown>(OnMirrorShutdown);
    }

    private void OnMobStateChanged(Entity<HeraldComponent> ent, ref ComponentShutdown args)
    {
        foreach (var mirror in ent.Comp.Mirrors)
            _destructible.BreakEntity(mirror);
    }

    private void OnTriShot(Entity<HeraldComponent> ent, ref HeraldTriShotActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var herald = ent.Owner;
        var target = args.Target;
        var shotCount = args.ShotCount;
        var spread = args.Spread;

        ShootVolley(herald, target, shotCount, spread);

        ExecuteMirrors(herald, target, (mirror, t) =>
        {
            ShootVolley(mirror, t, shotCount, spread);
        });

        if (IsLowHealth(herald, args.HealthThreshold))
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
            {
                if (!Exists(herald) || !Exists(target))
                    return;

                ShootVolley(herald, target, shotCount, spread);
            });
        }
    }

    private void OnSpreadShot(Entity<HeraldComponent> ent, ref HeraldSpreadShotActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var herald = ent.Owner;
        var shotDistance = args.ShotDistance;

        ShootSpread(herald, shotDistance);

        ExecuteMirrors(herald, herald, (mirror, _) =>
        {
            ShootSpread(mirror, shotDistance);
        });

        if (IsLowHealth(herald, args.HealthThreshold))
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
            {
                if (!Exists(herald))
                    return;

                ShootSpread(herald, shotDistance);
            });
        }
    }

    private void OnTeleShot(Entity<HeraldComponent> ent, ref HeraldTeleShotActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        ShootProjectile(ent.Owner, args.Target, args.TeleBoltPrototype);
    }

    private void OnMirror(Entity<HeraldComponent> ent, ref HeraldMirrorActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        if (ent.Comp.Mirrors.Count >= args.MaxMirrors)
            return;

        var mirror = Spawn(args.MirrorPrototype, Transform(ent.Owner).Coordinates);
        var mirrorComp = EnsureComp<HeraldMirrorComponent>(mirror);
        mirrorComp.OwnerHerald = ent.Owner;

        ent.Comp.Mirrors.Add(mirror);
    }

    private void OnMirrorShutdown(Entity<HeraldMirrorComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<HeraldComponent>(ent.Comp.OwnerHerald, out var comp))
            comp.Mirrors.Remove(ent.Owner);
    }

    #region Utility Methods

    private bool IsLowHealth(EntityUid uid, float threshold = 0.5f)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var maxHealth))
            return false;

        return totalDamage >= maxHealth.Value * threshold;
    }

    private void ShootProjectile(EntityUid shooter, EntityUid target, EntProtoId projectileProto)
    {
        if (!Exists(shooter) || !Exists(target))
            return;

        var shooterCoords = Transform(shooter).Coordinates;
        var targetPos = Transform(target).Coordinates.Position;
        var shooterPos = shooterCoords.Position;

        var direction = (targetPos - shooterPos).Normalized();
        var projectile = Spawn(projectileProto, shooterCoords);

        _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, shooter);
    }

    private void ShootVolley(EntityUid shooter, EntityUid target, int count, float spread = 0.3f)
    {
        if (!Exists(shooter) || !Exists(target))
            return;

        var shooterPos = Transform(shooter).Coordinates.Position;
        var targetPos = Transform(target).Coordinates.Position;
        var mapUid = _transform.GetMap(shooter);

        if (mapUid == null)
            return;

        var baseDirection = (targetPos - shooterPos).Normalized();

        for (int i = 0; i < count; i++)
        {
            var spreadX = baseDirection.X + _random.NextFloat(-spread, spread);
            var spreadY = baseDirection.Y + _random.NextFloat(-spread, spread);
            var direction = new Vector2(spreadX, spreadY).Normalized();

            var shotPos = shooterPos + direction * 5f;
            var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

            ShootAt(shooter, shotCoordinates);
        }
    }

    private void ShootSpread(EntityUid shooter, float distance = 8f)
    {
        if (!Exists(shooter))
            return;

        var shooterPos = Transform(shooter).Coordinates.Position;
        var mapUid = _transform.GetMap(shooter);

        if (mapUid == null)
            return;

        var directions = new[]
        {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0), new Vector2(1, 0),
            new Vector2(1, 1).Normalized(), new Vector2(1, -1).Normalized(),
            new Vector2(-1, 1).Normalized(), new Vector2(-1, -1).Normalized()
        };

        foreach (var dir in directions)
        {
            var shotPos = shooterPos + dir * distance;
            var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

            ShootAt(shooter, shotCoordinates);
        }
    }

    private void ShootAt(EntityUid shooter, EntityCoordinates targetCoordinates)
    {
        if (!TryComp<GunComponent>(shooter, out var gun))
            return;

        _gun.ForceShoot(shooter, shooter, gun, targetCoordinates);
    }

    private void ExecuteMirrors(EntityUid herald, EntityUid target, Action<EntityUid, EntityUid> action)
    {
        if (!TryComp<HeraldComponent>(herald, out var comp))
            return;

        foreach (var mirror in comp.Mirrors)
        {
            if (!Exists(mirror))
                continue;

            if (HasComp<HeraldMirrorComponent>(mirror))
                action(mirror, target);
        }
    }

    #endregion
}
