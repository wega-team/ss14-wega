using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Xenobiology;

public sealed class SlimeRebellionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SlimeSocialSystem _slimeSocial = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const int MaxSafeSlimes = 7;
    private const int MinRebellionGroup = 3;
    private const float BaseRebellionChance = 0.2f;
    private const float CheckInterval = 5f;
    private float _checkTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeRebellionComponent, ComponentShutdown>(OnRebellionEnd);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var rebellionData = new List<(EntityUid Uid, SlimeRebellionComponent Comp)>();
        var toRemove = new List<EntityUid>();

        var query = AllEntityQuery<SlimeRebellionComponent>();
        while (query.MoveNext(out var uid, out var rebellion))
        {
            if (_gameTiming.CurTime > rebellion.EndTime)
            {
                toRemove.Add(uid);
            }
            else
            {
                rebellionData.Add((uid, rebellion));
            }
        }

        foreach (var (uid, rebellion) in rebellionData)
        {
            SpreadRebellion(uid, rebellion);
        }

        if (toRemove.Count > 0)
        {
            foreach (var uid in toRemove)
            {
                if (Exists(uid))
                {
                    RemCompDeferred<SlimeRebellionComponent>(uid);
                }
            }
        }

        _checkTimer += frameTime;
        if (_checkTimer < CheckInterval) return;
        _checkTimer = 0f;

        CheckSlimeDensity();
    }

    private void CheckSlimeDensity()
    {
        var allSlimes = new List<(EntityUid Uid, TransformComponent Xform)>();
        var query = AllEntityQuery<SlimeSocialComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var slime, out var xform))
        {
            if (!HasComp<SlimeRebellionComponent>(uid))
                allSlimes.Add((uid, xform));
        }

        foreach (var (uid, xform) in allSlimes)
        {
            var nearbySlimes = _lookup.GetEntitiesInRange<SlimeSocialComponent>(
                xform.Coordinates,
                range: 4f);

            var validSlimes = nearbySlimes
                .Where(s => !HasComp<SlimeRebellionComponent>(s.Owner))
                .ToList();

            int count = validSlimes.Count;
            if (count < MinRebellionGroup)
                continue;

            var excess = count - MaxSafeSlimes;
            if (excess <= 0)
                continue;

            var rebellionChance = Math.Min(0.9f, excess * BaseRebellionChance);
            if (_random.Prob(rebellionChance))
            {
                var leader = validSlimes
                    .OrderBy(s => _slimeSocial.GetFriendsCount(s.Owner))
                    .ThenBy(_ => _random.NextFloat())
                    .First();

                _slimeSocial.StartRebellion(leader.Owner, count);
                break;
            }
        }
    }

    private void SpreadRebellion(EntityUid rebel, SlimeRebellionComponent rebellion)
    {
        var nearbySlimes = _lookup
            .GetEntitiesInRange<SlimeSocialComponent>(Transform(rebel).Coordinates, rebellion.SpreadRadius)
            .Where(s => !HasComp<SlimeRebellionComponent>(s))
            .ToList();

        var toJoin = new List<EntityUid>();

        foreach (var slime in nearbySlimes)
        {
            var social = Comp<SlimeSocialComponent>(slime);
            var friendFactor = social.Friends.Count * rebellion.FriendshipInfluence;
            var joinChance = rebellion.BaseJoinChance * (1f - Math.Clamp(friendFactor, 0f, 0.9f));
            if (_random.Prob(joinChance))
            {
                toJoin.Add(slime);
            }
        }

        foreach (var uid in toJoin)
        {
            _slimeSocial.JoinRebellion(uid, rebellion.Leader ?? rebel);
        }
    }

    private void OnRebellionEnd(EntityUid uid, SlimeRebellionComponent component, ComponentShutdown args)
    {
        if (!HasComp<SlimeSocialComponent>(uid))
            return;

        _slimeSocial.EndRebellion(uid);
    }
}
