using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Achievements;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class MegafaunaSystem : EntitySystem
{
    [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MegafaunaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MegafaunaComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MegafaunaComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(EntityUid uid, MegafaunaComponent component, MapInitEvent args)
    {
        if (!component.IsActive)
            _npc.SleepNPC(uid);
    }

    private void OnDamageChanged(EntityUid uid, MegafaunaComponent component, DamageChangedEvent args)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!component.IsActive && totalDamage > 0)
            ActivateMegafauna(uid, component);

        if (args.Origin != null && HasComp<ActorComponent>(args.Origin))
        {
            var damageContributor = EnsureComp<MegafaunaDamageContributorComponent>(uid);

            var damageDelta = args.DamageDelta?.GetTotal() ?? 0f;
            if (damageDelta > 0)
            {
                damageContributor.Contributors.TryGetValue(args.Origin.Value, out var current);
                damageContributor.Contributors[args.Origin.Value] = current + damageDelta;
                damageContributor.TotalDamageReceived += damageDelta;
            }
        }

        if (TryComp<HTNComponent>(uid, out var htn) && args.Origin != null)
            htn.Blackboard.SetValue(component.TargetKey, args.Origin.Value);
    }

    private void OnMobStateChanged(EntityUid uid, MegafaunaComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        GrantAchievementsToContributors(uid);

        HandleDeath(uid, args);
    }

    private void ActivateMegafauna(EntityUid uid, MegafaunaComponent component)
    {
        component.IsActive = true;
        if (component.AggroSound != null)
        {
            _audio.PlayGlobal(component.AggroSound, Filter.Pvs(uid), false);
        }

        if (component.BossMusic != null)
        {
            _ambient.SetSound(uid, component.BossMusic);
            _ambient.SetAmbience(uid, true);
        }

        _npc.WakeNPC(uid);
    }

    private void GrantAchievementsToContributors(EntityUid megafaunaUid)
    {
        if (HasComp<LegionBossComponent>(megafaunaUid)) // Specific
            return;

        if (!TryComp<MegafaunaDamageContributorComponent>(megafaunaUid, out var contributor))
            return;

        if (contributor.AchievementsGranted)
            return;

        contributor.AchievementsGranted = true;

        var totalDamage = contributor.TotalDamageReceived;
        if (totalDamage <= 0)
            return;

        var threshold = contributor.Threshold;
        var achievementId = contributor.AchievementId;

        foreach (var (player, damage) in contributor.Contributors)
        {
            var percentage = damage / totalDamage;
            if (percentage >= threshold)
            {
                _achievement.QueueAchievement(player, achievementId);
                _achievement.QueueAchievement(player, AchievementsEnum.FirstBoss);
            }
        }

        contributor.Contributors.Clear();
    }

    private void HandleDeath(EntityUid uid, MobStateChangedEvent args)
    {
        _ambient.SetAmbience(uid, false);

        var killedEvent = new MegafaunaKilledEvent
        {
            Megafauna = uid,
            Killer = args.Origin
        };
        RaiseLocalEvent(uid, ref killedEvent);
    }
}
