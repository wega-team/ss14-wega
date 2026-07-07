using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class FrostMinerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BossMusicSystem _bossMusic = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrostMinerIceOrbComponent, ComponentStartup>(OnOrbStartup);

        SubscribeLocalEvent<FrostMinerBossComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<FrostMinerBossComponent, FrostMinerIceOrbsActionEvent>(OnIceOrbs);
        SubscribeLocalEvent<FrostMinerBossComponent, FrostMinerSnowballActionEvent>(OnSnowball);
        SubscribeLocalEvent<FrostMinerBossComponent, FrostMinerShotgunActionEvent>(OnShotgun);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateOrbs();
    }

    #region Orb Logic

    private void OnOrbStartup(Entity<FrostMinerIceOrbComponent> ent, ref ComponentStartup args)
        => ent.Comp.SpawnTime = _timing.CurTime;

    private void UpdateOrbs()
    {
        var query = EntityQueryEnumerator<FrostMinerIceOrbComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var elapsed = _timing.CurTime - comp.SpawnTime;
            if (elapsed.TotalSeconds >= comp.ExplodeDelay)
                ExplodeIceOrb(uid, comp);
        }
    }

    private void ExplodeIceOrb(EntityUid orbUid, FrostMinerIceOrbComponent comp)
    {
        var coords = Transform(orbUid).Coordinates;
        EntityUid? shooter = Exists(comp.Shooter)
            ? comp.Shooter : null;

        for (int i = 0; i < comp.ShardsPerOrb; i++)
        {
            var angle = (i * MathF.PI * 2) / comp.ShardsPerOrb + _random.NextFloat(-0.2f, 0.2f);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var shard = Spawn(comp.ShardPrototype, coords);
            _gun.ShootProjectile(shard, direction, Vector2.Zero, null, shooter,
                SharedGunSystem.ProjectileSpeed / 4);
        }

        Del(orbUid);
    }

    #endregion

    private void OnMobStateChanged(Entity<FrostMinerBossComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical && !ent.Comp.IsTransitioning)
        {
            ent.Comp.IsTransitioning = true;
            TransformToDemon(ent);
        }
    }

    private void TransformToDemon(Entity<FrostMinerBossComponent> ent)
    {
        var coords = Transform(ent).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var damageContrib = Comp<MegafaunaDamageContributorComponent>(ent);
        var demon = Spawn(ent.Comp.DemonPrototype, coords);
        _bossMusic.TransferBossMusic(ent.Owner, demon);

        var demonContrib = EnsureComp<MegafaunaDamageContributorComponent>(demon);
        demonContrib.TotalDamageReceived = damageContrib.TotalDamageReceived;
        demonContrib.Contributors = damageContrib.Contributors;

        QueueDel(ent);
    }

    private void OnIceOrbs(Entity<FrostMinerBossComponent> ent, ref FrostMinerIceOrbsActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var shooter = ent.Owner;
        var target = args.Target;
        var orbProto = args.OrbPrototype;
        var explodeDelay = args.ExplodeDelay;
        var shardsPerOrb = args.ShardsPerOrb;

        for (int i = 0; i < args.Count; i++)
        {
            var angle = (i * MathF.PI * 2) / args.Count;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var delay = i * args.SpawnDelay;
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Exists(shooter) || _mobState.IsDead(shooter) || !Exists(target))
                    return;

                var spawnPos = Transform(shooter).Coordinates.Offset(direction * 3f);
                var orb = Spawn(orbProto, spawnPos);

                var orbComp = EnsureComp<FrostMinerIceOrbComponent>(orb);
                orbComp.Shooter = shooter;
                orbComp.ExplodeDelay = explodeDelay;
                orbComp.ShardsPerOrb = shardsPerOrb;

                _gun.ShootProjectile(orb, direction, Vector2.Zero, null, shooter,
                    SharedGunSystem.ProjectileSpeed * 0.02f);
            });
        }
    }

    private void OnSnowball(Entity<FrostMinerBossComponent> ent, ref FrostMinerSnowballActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var shooter = ent.Owner;
        var target = args.Target;

        var count = args.Count;
        var spread = args.Spread;
        var snowballProto = args.SnowballPrototype;
        var waveCount = args.WaveCount;
        var waveShards = args.WaveShards;
        var shardProto = args.ShardPrototype;

        for (int i = 0; i < count; i++)
        {
            var delay = i * 0.05f;
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Exists(shooter) || _mobState.IsDead(shooter) || !Exists(target))
                    return;

                ShootSnowball(shooter, target, spread, snowballProto);
            });
        }

        for (int wave = 0; wave < waveCount; wave++)
        {
            var currentWave = wave;
            var totalDelay = currentWave * 0.3f + count * 0.05f;
            Timer.Spawn(TimeSpan.FromSeconds(totalDelay), () =>
            {
                if (!Exists(shooter) || _mobState.IsDead(shooter))
                    return;

                ShootIceWave(shooter, waveShards, shardProto);
            });
        }
    }

    private void ShootSnowball(EntityUid shooter, EntityUid target, float spread, EntProtoId prototype)
    {
        if (!Exists(shooter) || !Exists(target))
            return;

        var shooterPos = Transform(shooter).Coordinates.Position;
        var targetPos = Transform(target).Coordinates.Position;
        var baseDirection = (targetPos - shooterPos).Normalized();

        var spreadRad = MathF.PI / 180 * spread;
        var spreadX = baseDirection.X + _random.NextFloat(-spreadRad / 2, spreadRad / 2);
        var spreadY = baseDirection.Y + _random.NextFloat(-spreadRad / 2, spreadRad / 2);
        var direction = new Vector2(spreadX, spreadY).Normalized();

        var snowball = Spawn(prototype, Transform(shooter).Coordinates);
        _gun.ShootProjectile(snowball, direction, Vector2.Zero, null, shooter,
            SharedGunSystem.ProjectileSpeed * 0.2f);
    }

    private void ShootIceWave(EntityUid shooter, int shardCount, EntProtoId shardPrototype)
    {
        if (!Exists(shooter))
            return;

        for (int i = 0; i < shardCount; i++)
        {
            var angle = (i * MathF.PI * 2) / shardCount;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var shard = Spawn(shardPrototype, Transform(shooter).Coordinates);
            _gun.ShootProjectile(shard, direction, Vector2.Zero, null, shooter,
                SharedGunSystem.ProjectileSpeed * 0.2f);
        }
    }

    private void OnShotgun(Entity<FrostMinerBossComponent> ent, ref FrostMinerShotgunActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var shooter = ent.Owner;
        var target = args.Target;
        var spread = args.Spread;
        var shardProto = args.ShardPrototype;

        var alternatingCounts = new List<int>();
        for (int i = 0; i < args.Waves; i++)
        {
            alternatingCounts.Add(i % 2 == 0 ? args.WaveCount : Math.Max(1, args.WaveCount - 1));
        }

        for (int wave = 0; wave < args.Waves; wave++)
        {
            var currentWave = wave;
            var count = alternatingCounts[wave];

            Timer.Spawn(TimeSpan.FromSeconds(currentWave * 0.25f), () =>
            {
                if (!Exists(shooter) || _mobState.IsDead(shooter) || !Exists(target))
                    return;

                ShootShotgunWave(shooter, target, count, spread, shardProto);
            });
        }
    }

    private void ShootShotgunWave(EntityUid shooter, EntityUid target, int count, float spread, EntProtoId prototype)
    {
        var shooterPos = Transform(shooter).Coordinates.Position;
        var targetPos = Transform(target).Coordinates.Position;
        var baseDirection = (targetPos - shooterPos).Normalized();

        if (spread >= 360f)
        {
            for (int i = 0; i < count; i++)
            {
                var angle = (i * MathF.PI * 2) / count + _random.NextFloat(-0.1f, 0.1f);
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var shard = Spawn(prototype, Transform(shooter).Coordinates);
                _gun.ShootProjectile(shard, direction, Vector2.Zero, null, shooter,
                    SharedGunSystem.ProjectileSpeed * 0.2f);
            }
            return;
        }

        var spreadRad = MathF.PI / 180 * spread;
        for (int i = 0; i < count; i++)
        {
            var randomOffset = _random.NextFloat(-spreadRad / 2, spreadRad / 2);
            var direction = new Vector2(
                baseDirection.X * MathF.Cos(randomOffset) - baseDirection.Y * MathF.Sin(randomOffset),
                baseDirection.X * MathF.Sin(randomOffset) + baseDirection.Y * MathF.Cos(randomOffset)
            ).Normalized();

            var shard = Spawn(prototype, Transform(shooter).Coordinates);
            _gun.ShootProjectile(shard, direction, Vector2.Zero, null, shooter,
                SharedGunSystem.ProjectileSpeed * 0.2f);
        }
    }
}
