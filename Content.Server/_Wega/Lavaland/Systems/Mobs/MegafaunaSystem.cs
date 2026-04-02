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

        if (TryComp<HTNComponent>(uid, out var htn) && args.Origin != null)
            htn.Blackboard.SetValue(component.TargetKey, args.Origin.Value);
    }

    private void OnMobStateChanged(EntityUid uid, MegafaunaComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

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

    private void HandleDeath(EntityUid uid, MobStateChangedEvent args)
    {
        _ambient.SetAmbience(uid, false);
        if (args.Origin != null && !HasComp<LegionBossComponent>(uid))
        {
            _achievement.QueueAchievement(args.Origin.Value, AchievementsEnum.FirstBoss);
        }

        var killedEvent = new MegafaunaKilledEvent
        {
            Megafauna = uid,
            Killer = args.Origin
        };
        RaiseLocalEvent(uid, ref killedEvent);
    }
}
