using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Achievements;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland;

public sealed class ColossusSystem : EntitySystem
{
    [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColossusBossComponent, MegafaunaKilledEvent>(OnColossusKilled);

        SubscribeLocalEvent<ColossusBossComponent, ColossusFractionActionEvent>(OnFractionAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusCrossActionEvent>(OnCrossAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusSpiralActionEvent>(OnSpiralAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusTripleFractionActionEvent>(OnTripleFractionAction);
    }

    private void OnColossusKilled(EntityUid uid, ColossusBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        foreach (var reward in component.RewardsProto)
            Spawn(reward, coords);

        QueueDel(uid);
        if (args.Killer != null)
        {
            _achievement.QueueAchievement(args.Killer.Value, AchievementsEnum.ColossusBoss);
        }
    }

    private void OnFractionAction(Entity<ColossusBossComponent> ent, ref ColossusFractionActionEvent args)
    {
        args.Handled = true;

        ShootFraction(ent.Owner, args.Target, args.FractionSpread, args.FractionCount);
    }

    private void OnCrossAction(Entity<ColossusBossComponent> ent, ref ColossusCrossActionEvent args)
    {
        args.Handled = true;

        var length = args.CrossLength;
        ShootCross(ent.Owner, length);
        Timer.Spawn(TimeSpan.FromSeconds(args.CrossDelay), () =>
        {
            if (Exists(ent.Owner))
            {
                ShootDiagonals(ent.Owner, length);
            }
        });
    }

    private void OnSpiralAction(Entity<ColossusBossComponent> ent, ref ColossusSpiralActionEvent args)
    {
        args.Handled = true;

        if (_threshold.TryGetThresholdForState(ent, MobState.Dead, out var threshold)
            && TryComp<DamageableComponent>(ent, out var damageable))
        {
            var totalDamage = _damage.GetTotalDamage((ent.Owner, damageable));
            if (totalDamage > 0 && totalDamage >= threshold - threshold * args.DieHealthModifier)
            {
                _chat.TrySendInGameICMessage(ent.Owner, "DIE", InGameICChatType.Speak, false, true, ignoreActionBlocker: true);

                ShootSpiral(ent.Owner, args.DieProjectileCount,
                    args.DieProjectileDelay, true);

                return;
            }
        }

        _chat.TrySendInGameICMessage(ent.Owner, "JUDGEMENT", InGameICChatType.Speak, false, true, ignoreActionBlocker: true);

        ShootSpiral(ent.Owner, args.JudgementProjectileCount,
            args.JudgementProjectileDelay, false);
    }

    private void OnTripleFractionAction(Entity<ColossusBossComponent> ent, ref ColossusTripleFractionActionEvent args)
    {
        args.Handled = true;

        ShootTripleFraction(ent.Owner, args.Target, args.FractionSpread,
            args.FractionCount, args.TripleFractionDelay);
    }

    private void ShootFraction(EntityUid colossus, EntityUid target, float spread, int count)
    {
        var colossusWorldPos = _transform.GetWorldPosition(colossus);
        var targetWorldPos = _transform.GetWorldPosition(target);

        var direction = (targetWorldPos - colossusWorldPos).Normalized();

        var mapUid = _transform.GetMap(colossus);
        if (mapUid == null)
            return;

        for (int i = 0; i < count; i++)
        {
            var spreadX = direction.X + _random.NextFloat(-spread, spread);
            var spreadY = direction.Y + _random.NextFloat(-spread, spread);
            var spreadDirection = new Vector2(spreadX, spreadY).Normalized();

            var shotDistance = 5f;
            var shotPos = colossusWorldPos + spreadDirection * shotDistance;

            var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

            ShootAt(colossus, shotCoordinates);
        }
    }

    private void ShootCross(EntityUid colossus, float distance)
    {
        var colossusWorldPos = _transform.GetWorldPosition(colossus);

        var mapUid = _transform.GetMap(colossus);
        if (mapUid == null)
            return;

        var directions = new[]
        {
            new Vector2(0, 1),
            new Vector2(0, -1),
            new Vector2(-1, 0),
            new Vector2(1, 0)
        };

        foreach (var dir in directions)
        {
            var shotPos = colossusWorldPos + dir * distance;
            var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

            ShootAt(colossus, shotCoordinates);
        }
    }

    private void ShootDiagonals(EntityUid colossus, float distance)
    {
        var colossusWorldPos = _transform.GetWorldPosition(colossus);

        var mapUid = _transform.GetMap(colossus);
        if (mapUid == null)
            return;

        var diagonals = new[]
        {
            new Vector2(1, 1).Normalized(),
            new Vector2(1, -1).Normalized(),
            new Vector2(-1, -1).Normalized(),
            new Vector2(-1, 1).Normalized()
        };

        foreach (var dir in diagonals)
        {
            var shotPos = colossusWorldPos + dir * distance;
            var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

            ShootAt(colossus, shotCoordinates);
        }
    }

    private void ShootSpiral(EntityUid colossus, int projectileCount, float delay, bool dualSpiral)
    {
        var colossusWorldPos = _transform.GetWorldPosition(colossus);

        var mapUid = _transform.GetMap(colossus);
        if (mapUid == null)
            return;

        var startAngle = _random.NextFloat(0, MathF.PI * 2);

        var spiralCount = dualSpiral ? 2 : 1;
        for (int spiral = 0; spiral < spiralCount; spiral++)
        {
            var spiralOffset = spiral == 0 ? 0f : MathF.PI;

            var spiralTurns = 3f;
            var totalAngle = spiralTurns * MathF.PI * 2;

            var denseProjectileCount = projectileCount * 3;
            for (int i = 0; i < denseProjectileCount; i++)
            {
                var progress = (float)i / (denseProjectileCount - 1);

                var angle = startAngle + progress * totalAngle + spiralOffset;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                var minDistance = 1.5f;
                var maxDistance = 8f;
                var distance = minDistance + progress * (maxDistance - minDistance);

                var shotPos = colossusWorldPos + direction * distance;

                var shotCoordinates = new EntityCoordinates(mapUid.Value, shotPos);

                var fastDelay = delay * 0.5f;
                var timer = i * fastDelay;

                Timer.Spawn(TimeSpan.FromSeconds(timer), () =>
                {
                    if (Exists(colossus))
                    {
                        ShootAt(colossus, shotCoordinates);
                    }
                });
            }
        }
    }

    private void ShootTripleFraction(EntityUid colossus, EntityUid target,
        float spread, int count, float delay)
    {
        for (int volley = 0; volley < 3; volley++)
        {
            var timer = volley * delay;
            Timer.Spawn(TimeSpan.FromSeconds(timer), () =>
            {
                if (Exists(colossus) && Exists(target))
                {
                    ShootFraction(colossus, target, spread, count);
                }
            });
        }
    }

    private void ShootAt(EntityUid colossus, EntityCoordinates targetCoordinates)
    {
        if (!TryComp<GunComponent>(colossus, out var gun))
            return;

        _gun.ForceShoot(colossus, colossus, gun, targetCoordinates);
    }
}
