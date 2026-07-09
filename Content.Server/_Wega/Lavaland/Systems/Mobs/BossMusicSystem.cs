using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using System.Numerics;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class BossMusicSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float SilenceVolume = -30f;
    private float _maxDistance = 21f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.ViewportMaximumWidth, value => _maxDistance = value, true);

        SubscribeLocalEvent<BossMusicComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BossMusicComponent, MegafaunaKilledEvent>(OnBossKilled,
            after: [typeof(LegionSystem)]);

        SubscribeLocalEvent<BossMusicTrackerComponent, ComponentShutdown>(OnPlayerShutdown);
        SubscribeLocalEvent<BossMusicTrackerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BossMusicTrackerComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<BossMusicTrackerComponent, MobStateChangedEvent>(OnPlayerStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Well, God willing, even if there are 3-6 of them at the same time. It's not that critical.
        var query = EntityQueryEnumerator<BossMusicTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
            UpdateTracker(uid, tracker, frameTime);
    }

    private void OnDamageChanged(Entity<BossMusicComponent> boss, ref DamageChangedEvent args)
    {
        if (args.Origin == null || !HasComp<ActorComponent>(args.Origin))
            return;

        var player = args.Origin.Value;
        StartBossMusic(player, boss.Owner);
    }

    private void OnBossKilled(Entity<BossMusicComponent> boss, ref MegafaunaKilledEvent args)
    {
        if (HasComp<LegionBossComponent>(boss)) // Specific
        {
            var query = EntityQueryEnumerator<BossMusicTrackerComponent>();
            while (query.MoveNext(out var uid, out var tracker))
            {
                if (tracker.Boss == boss.Owner)
                {
                    var nearestSplit = FindNearestLegionSplit(uid, boss.Owner);
                    if (nearestSplit != null)
                    {
                        tracker.Boss = nearestSplit.Value;
                    }
                    else
                    {
                        StopTracker(uid, tracker);
                    }
                }
            }
            return;
        }

        StopMusicForBoss(boss.Owner);
    }

    private void OnPlayerShutdown(Entity<BossMusicTrackerComponent> tracker, ref ComponentShutdown args)
    {
        if (tracker.Comp.AudioEntity != null && Exists(tracker.Comp.AudioEntity))
            _audio.Stop(tracker.Comp.AudioEntity.Value);
    }

    // This is if the user quickly connected back, for example with a disconnect.
    private void OnPlayerAttached(Entity<BossMusicTrackerComponent> tracker, ref PlayerAttachedEvent args)
    {
        var player = tracker.Owner;
        var boss = tracker.Comp.Boss;

        StopTracker(player, tracker, true);
        if (Exists(boss) && !_mobState.IsDead(boss))
            StartBossMusic(player, boss);
    }

    private void OnPlayerDetached(Entity<BossMusicTrackerComponent> tracker, ref PlayerDetachedEvent args)
        => StopTracker(tracker.Owner, tracker);

    private void OnPlayerStateChanged(Entity<BossMusicTrackerComponent> tracker, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead || args.NewMobState == MobState.Critical)
            StopTracker(tracker.Owner, tracker);
    }

    public void StartBossMusic(EntityUid player, EntityUid boss)
    {
        if (!TryComp<BossMusicComponent>(boss, out var bossMusic))
            return;

        if (TryComp<BossMusicTrackerComponent>(player, out var existingTracker) && existingTracker.Boss == boss)
            return;

        if (existingTracker != null)
        {
            if (HasComp<LegionBossComponent>(existingTracker.Boss) && HasComp<LegionBossComponent>(boss))
            {
                existingTracker.Boss = boss;
                return;
            }

            StopTracker(player, existingTracker, true);
        }

        var tracker = EnsureComp<BossMusicTrackerComponent>(player);
        tracker.Boss = boss;

        var resolvedSound = _audio.ResolveSound(bossMusic.Music);
        var audioParams = new AudioParams
        {
            Volume = bossMusic.Volume,
            Loop = true,
        };

        var audioEntity = _audio.PlayGlobal(resolvedSound, player, audioParams);
        if (audioEntity != null)
        {
            tracker.AudioEntity = audioEntity.Value.Entity;
            tracker.CurrentVolume = bossMusic.Volume;
            tracker.IsFadingOut = false;
        }
        else
        {
            RemComp<BossMusicTrackerComponent>(player);
        }
    }

    public void TransferBossMusic(EntityUid fromBoss, EntityUid toBoss)
    {
        if (!HasComp<BossMusicComponent>(toBoss))
            return;

        var query = EntityQueryEnumerator<BossMusicTrackerComponent>();
        while (query.MoveNext(out _, out var tracker))
        {
            if (tracker.Boss == fromBoss && tracker.AudioEntity != null)
                tracker.Boss = toBoss;
        }
    }

    public void StopTracker(EntityUid player, BossMusicTrackerComponent? tracker = null, bool instant = false)
    {
        if (!Resolve(player, ref tracker, false) || tracker.IsFadingOut && !instant)
            return;

        if (tracker.AudioEntity == null)
        {
            RemComp<BossMusicTrackerComponent>(player);
            return;
        }

        if (instant)
        {
            _audio.Stop(tracker.AudioEntity);
            RemComp<BossMusicTrackerComponent>(player);
            return;
        }

        tracker.IsFadingOut = true;
    }

    public void StopMusicForBoss(EntityUid boss)
    {
        var query = EntityQueryEnumerator<BossMusicTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (tracker.Boss == boss)
            {
                StopTracker(uid, tracker);
            }
        }
    }

    private void UpdateTracker(EntityUid player, BossMusicTrackerComponent tracker, float frameTime)
    {
        if (tracker.AudioEntity == null || !Exists(tracker.AudioEntity.Value))
        {
            RemComp<BossMusicTrackerComponent>(player);
            return;
        }

        // Moshi Moshi. I see that you're looking here.
        // I know you're looking here. Therefore, the unexpected "UwU"/

        if (tracker.IsFadingOut)
        {
            var fadeStep = frameTime / 5f;
            tracker.CurrentVolume -= fadeStep * 10f;
            if (tracker.CurrentVolume <= SilenceVolume)
            {
                _audio.Stop(tracker.AudioEntity);
                RemComp<BossMusicTrackerComponent>(player);
                return;
            }

            _audio.SetVolume(tracker.AudioEntity, tracker.CurrentVolume);
            return;
        }

        if (!Exists(tracker.Boss) || _mobState.IsDead(tracker.Boss))
        {
            StopTracker(player, tracker);
            return;
        }

        var playerPos = _transform.GetMapCoordinates(player);
        var bossPos = _transform.GetMapCoordinates(tracker.Boss);
        if (playerPos.MapId != bossPos.MapId)
        {
            if (!tracker.IsFadingOut)
            {
                StopTracker(player, tracker);
            }
            return;
        }

        var distance = Vector2.Distance(playerPos.Position, bossPos.Position);
        if (distance >= _maxDistance)
        {
            if (!tracker.IsFadingOut)
            {
                StopTracker(player, tracker);
            }
        }
    }

    // Specific
    private EntityUid? FindNearestLegionSplit(EntityUid player, EntityUid deadBoss)
    {
        var playerPos = _transform.GetMapCoordinates(player);

        EntityUid? nearestSplit = null;
        var nearestDistance = float.MaxValue;

        var splitQuery = EntityQueryEnumerator<LegionBossComponent>();
        while (splitQuery.MoveNext(out var uid, out var _))
        {
            if (uid == deadBoss)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            var splitPos = _transform.GetMapCoordinates(uid);
            if (playerPos.MapId != splitPos.MapId)
                continue;

            var distance = Vector2.Distance(playerPos.Position, splitPos.Position);
            if (distance > _maxDistance)
                continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSplit = uid;
            }
        }

        return nearestSplit;
    }
}
