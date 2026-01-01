using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Lavaland.Mobs;
using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class MegafaunaSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;

    private static readonly ProtoId<NpcFactionPrototype> Fauna = "LavalandFauna";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MegafaunaComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MegafaunaComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateMegafaunaState();
        UpdateAttacks(frameTime);
    }

    private void UpdateMegafaunaState()
    {
        var query = EntityQueryEnumerator<MegafaunaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (ShouldAutoAggro(uid))
            {
                if (!comp.IsActive)
                {
                    ActivateMegafauna(uid, comp);
                }
            }

            if (!comp.IsActive && HasAggressors(uid))
            {
                ActivateMegafauna(uid, comp);
            }
        }
    }

    private void UpdateAttacks(float frameTime)
    {
        var query = EntityQueryEnumerator<MegafaunaAttacksComponent, MegafaunaComponent>();
        while (query.MoveNext(out var uid, out var attacks, out var comp))
        {
            if (!comp.IsActive)
                continue;

            attacks.NextAttackTime -= TimeSpan.FromSeconds(frameTime);

            if (attacks.NextAttackTime <= TimeSpan.Zero)
            {
                TryExecuteAttack(uid, attacks, comp);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, MegafaunaComponent component, DamageChangedEvent args)
    {
        if (args.Origin != null && Exists(args.Origin.Value))
        {
            AddAggressor(uid, args.Origin.Value);
        }

        if (!component.IsActive && args.Damageable.TotalDamage > 0)
        {
            ActivateMegafauna(uid, component);
        }
    }

    private void OnMobStateChanged(EntityUid uid, MegafaunaComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        HandleDeath(uid, component, args);
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

        var startupEvent = new MegafaunaStartupEvent();
        RaiseLocalEvent(uid, ref startupEvent);
    }

    private void HandleDeath(EntityUid uid, MegafaunaComponent component, MobStateChangedEvent args)
    {
        _ambient.SetAmbience(uid, false);

        var killedEvent = new MegafaunaKilledEvent
        {
            Megafauna = uid,
            Killer = args.Origin
        };
        RaiseLocalEvent(uid, ref killedEvent);

        var deinitEvent = new MegafaunaDeinitEvent();
        RaiseLocalEvent(uid, ref deinitEvent);
    }

    private void AddAggressor(EntityUid uid, EntityUid aggressor)
    {
        if (!TryComp<MegafaunaAwarenessComponent>(uid, out var awareness) || HasComp<MegafaunaComponent>(aggressor)
            || _factionSystem.IsMember((aggressor, null), Fauna))
            return;

        if (!awareness.Aggressors.Contains(aggressor))
            awareness.Aggressors.Add(aggressor);
    }

    private bool HasAggressors(EntityUid uid)
    {
        return TryComp<MegafaunaAwarenessComponent>(uid, out var awareness)
            && awareness.Aggressors.Count > 0;
    }

    private bool ShouldAutoAggro(EntityUid uid)
    {
        if (!TryComp<MegafaunaAwarenessComponent>(uid, out var awareness) || !awareness.AutoAggro)
            return false;

        var selfCoords = Transform(uid).Coordinates;
        var players = _lookup.GetEntitiesInRange<MobStateComponent>(selfCoords, awareness.AggroRange)
            .Where(x => !HasComp<MegafaunaComponent>(x) && x.Owner != uid).ToList();

        bool added = false;
        foreach (var player in players)
        {
            if (!IsValidTarget(uid, player.Owner))
                continue;

            AddAggressor(uid, player.Owner);
            added = true;
        }

        return added;
    }

    private void TryExecuteAttack(EntityUid uid, MegafaunaAttacksComponent attacks, MegafaunaComponent comp)
    {
        var target = FindAttackTarget(uid);
        if (target == null)
            return;

        var targetCoords = Transform(target.Value).Coordinates;
        var selfCoords = Transform(uid).Coordinates;

        if (!targetCoords.TryDistance(EntityManager, selfCoords, out var distance)
            || distance > attacks.AttackRange)
            return;

        var attackEvent = new MegafaunaAttackEvent(target.Value);
        RaiseLocalEvent(uid, ref attackEvent);

        attacks.NextAttackTime = TimeSpan.FromSeconds(attacks.BaseAttackCooldown);
    }

    public EntityUid? FindAttackTarget(EntityUid uid)
    {
        if (!TryComp<MegafaunaAwarenessComponent>(uid, out var awareness))
            return null;

        if (awareness.Aggressors.Count == 0)
            return null;

        awareness.Aggressors.RemoveAll(aggressor =>
            !IsValidTarget(uid, aggressor)
        );

        return awareness.Aggressors.Count > 0 ?
            awareness.Aggressors[_random.Next(awareness.Aggressors.Count)] : null;
    }

    private bool IsValidTarget(EntityUid uid, EntityUid aggressor)
    {
        if (!Exists(aggressor) || HasComp<MegafaunaComponent>(aggressor)
            || _factionSystem.IsMember((aggressor, null), Fauna))
            return false;

        if (_mobState.IsDead(aggressor) || _mobState.IsCritical(aggressor))
            return false;

        var selfCoords = Transform(uid).Coordinates;
        var targetCoords = Transform(aggressor).Coordinates;

        var distance = (targetCoords.Position - selfCoords.Position).Length();
        if (distance > 25f)
            return false;

        return true;
    }
}
