using System.Linq;
using System.Numerics;
using Content.Server.Implants;
using Content.Shared._Wega.Seraphim;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Drunk;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Wega.Seraphim;

public sealed partial class SeraphimSystem : SharedSeraphimSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SubdermalImplantSystem _implant = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    private const string KneeType = "Knee";
    private const string FuckOffType = "FuckOff";

    private static readonly TimeSpan PreparedDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan KneeCooldown = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan KneeSlowDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FuckOffCooldown = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan ConcentrateDuration = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ConcentrateCooldown = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan OverdriveDuration = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan OverdriveCooldown = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan ExhaustedDuration = TimeSpan.FromSeconds(20);

    private static readonly ProtoId<ReagentPrototype> SpaceDrugs = "SpaceDrugs";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeraphimImplantComponent, SeraphimKneeActionEvent>(OnKneeAction);
        SubscribeLocalEvent<SeraphimImplantComponent, SeraphimFuckOffActionEvent>(OnFuckOffAction);
        SubscribeLocalEvent<SeraphimImplantComponent, SeraphimConcentrateActionEvent>(OnConcentrateAction);
        SubscribeLocalEvent<SeraphimImplantComponent, SeraphimOverdriveActionEvent>(OnOverdriveAction);

        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<SeraphimImplantComponent, ComponentInit>(OnImplantInit);
        SubscribeLocalEvent<SeraphimImplantComponent, ComponentShutdown>(OnImplantShutdown);

        SubscribeLocalEvent<SeraphimConcentratingComponent, ComponentShutdown>(OnConcentrateShutdown);

        SubscribeLocalEvent<SeraphimImplanterComponent, GetVerbsEvent<AlternativeVerb>>(OnImplanterVerbs);
        SubscribeLocalEvent<SeraphimImplanterComponent, SeraphimImplantDoAfterEvent>(OnImplantDoAfter);
    }

    private void OnKneeAction(Entity<SeraphimImplantComponent> ent, ref SeraphimKneeActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var prep = EnsureComp<SeraphimPreparedComponent>(ent);
        prep.Type = KneeType;
        prep.ExpireTime = _timing.CurTime + PreparedDuration;
        Dirty(ent.Owner, prep);

        _popup.PopupEntity(Loc.GetString("seraphim-knee-prepared"), ent, ent, PopupType.Medium);
        _actions.SetCooldown(args.Action.Owner, KneeCooldown);
        args.Handled = true;
    }

    private void OnFuckOffAction(Entity<SeraphimImplantComponent> ent, ref SeraphimFuckOffActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var prep = EnsureComp<SeraphimPreparedComponent>(ent);
        prep.Type = FuckOffType;
        prep.ExpireTime = _timing.CurTime + PreparedDuration;
        Dirty(ent.Owner, prep);

        _popup.PopupEntity(Loc.GetString("seraphim-fuckoff-prepared"), ent, ent, PopupType.Medium);
        _actions.SetCooldown(args.Action.Owner, FuckOffCooldown);
        args.Handled = true;
    }

    private void OnConcentrateAction(Entity<SeraphimImplantComponent> ent, ref SeraphimConcentrateActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        EntityUid? blade = null;
        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            if (HasComp<CorporateBladeComponent>(held))
            {
                blade = held;
                break;
            }
        }

        if (blade == null)
        {
            _popup.PopupEntity(Loc.GetString("seraphim-need-blade"), ent, ent, PopupType.Medium);
            return;
        }

        var comp = EnsureComp<SeraphimConcentratingComponent>(ent);
        comp.EndTime = _timing.CurTime + ConcentrateDuration;
        comp.Weapon = blade;

        var reflect = EnsureComp<ReflectComponent>(blade.Value);
        comp.OldReflectProb = reflect.ReflectProb;
        reflect.ReflectProb = comp.BuffReflectProb;
        Dirty(blade.Value, reflect);

        comp.AddedPacified = !HasComp<PacifiedComponent>(ent);
        if (comp.AddedPacified)
            EnsureComp<PacifiedComponent>(ent);

        Dirty(ent.Owner, comp);

        _popup.PopupEntity(Loc.GetString("seraphim-concentrate-start"), ent, ent, PopupType.Medium);
        _actions.SetCooldown(args.Action.Owner, ConcentrateCooldown);
        args.Handled = true;
    }

    private void OnOverdriveAction(Entity<SeraphimImplantComponent> ent, ref SeraphimOverdriveActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var comp = EnsureComp<SeraphimOverdriveComponent>(ent);
        comp.EndTime = _timing.CurTime + OverdriveDuration;
        Dirty(ent.Owner, comp);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        _chat.TryEmoteWithChat(ent, "Scream", ChatTransmitRange.Normal, ignoreActionBlocker: true, forceEmote: true);
        _popup.PopupEntity(Loc.GetString("seraphim-overdrive-start"), ent, PopupType.LargeCaution);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Wega/Effects/seraphim/seraphim_working.ogg"), ent);
        _jitter.DoJitter(ent, OverdriveDuration, true, 10f, 8f);

        _actions.SetCooldown(args.Action.Owner, OverdriveCooldown);
        args.Handled = true;
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !TryComp<SeraphimPreparedComponent>(args.User, out var prep))
            return;

        var target = args.HitEntities.FirstOrNull();
        if (target == null)
            return;

        // Удар в колено - только кулаком
        if (prep.Type == KneeType && args.User != args.Weapon)
            return;

        switch (prep.Type)
        {
            case KneeType:
                var slow = EnsureComp<SeraphimSlowedComponent>(target.Value);
                slow.EndTime = _timing.CurTime + KneeSlowDuration;
                Dirty(target.Value, slow);
                _speed.RefreshMovementSpeedModifiers(target.Value);
                _stamina.TakeStaminaDamage(target.Value, 20f, source: args.User);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Wega/Effects/seraphim/hit_kick.ogg"), target.Value);
                _popup.PopupEntity(Loc.GetString("seraphim-knee-hit", ("target", target.Value)), args.User, args.User, PopupType.Medium);
                break;

            case FuckOffType:
                var dir = _transform.GetWorldPosition(target.Value) - _transform.GetWorldPosition(args.User);
                if (dir.LengthSquared() > 0f)
                {
                    dir = Vector2.Normalize(dir);
                    _throwing.TryThrow(target.Value, dir * 5f, 10f, args.User);
                }
                _stamina.TakeStaminaDamage(target.Value, 30f, source: args.User);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Wega/Effects/seraphim/hit_punch.ogg"), target.Value);
                _popup.PopupEntity(Loc.GetString("seraphim-fuckoff-hit", ("target", target.Value)), args.User, args.User, PopupType.Medium);
                break;

            default:
                return;
        }

        RemComp<SeraphimPreparedComponent>(args.User);
    }

    private void OnConcentrateShutdown(Entity<SeraphimConcentratingComponent> ent, ref ComponentShutdown args)
    {
        // Возвращаем рефлект
        if (ent.Comp.Weapon is { } weapon && TryComp<ReflectComponent>(weapon, out var reflect))
        {
            reflect.ReflectProb = ent.Comp.OldReflectProb;
            Dirty(weapon, reflect);
        }

        // Возвращаем пацифизм
        if (ent.Comp.AddedPacified && !TerminatingOrDeleted(ent))
            RemComp<PacifiedComponent>(ent);
    }

    // Экшены
    private void OnImplantInit(Entity<SeraphimImplantComponent> ent, ref ComponentInit args)
    {
        foreach (var actionId in ent.Comp.ActionIds)
        {
            ent.Comp.Actions.Add(_actions.AddAction(ent, actionId));
        }
    }

    private void OnImplantShutdown(Entity<SeraphimImplantComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actions.RemoveAction(ent.Owner, action);
        }
    }

    // Кто пристёгнут
    private EntityUid? GetBuckledTarget(EntityUid strap)
    {
        var query = EntityQueryEnumerator<BuckleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BuckledTo == strap)
                return uid;
        }

        return null;
    }

    // Автоимплантер
    private void OnImplanterVerbs(Entity<SeraphimImplanterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || ent.Comp.Used || ent.Comp.Active)
            return;

        if (GetBuckledTarget(ent.Owner) is not { } target)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("seraphim-implanter-verb"),
            Priority = 10,
            Act = () => StartImplant(ent, target),
        });
    }

    private void StartImplant(Entity<SeraphimImplanterComponent> ent, EntityUid target)
    {
        if (ent.Comp.Used || ent.Comp.Active)
            return;

        if (!HasComp<VisualBodyComponent>(target) || HasComp<SeraphimImplantComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("seraphim-implanter-invalid"), ent, PopupType.Medium);
            return;
        }

        ent.Comp.Active = true;
        ent.Comp.Target = target;
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        ent.Comp.NextEffectTime = _timing.CurTime + ent.Comp.EffectInterval;

        // Прогресс-бар на пациенте
        var doAfterArgs = new DoAfterArgs(EntityManager, target, ent.Comp.Duration, new SeraphimImplantDoAfterEvent(), ent.Owner, target: ent.Owner)
        {
            NeedHand = false,
            BreakOnMove = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
        };
        if (_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
            ent.Comp.DoAfter = doAfterId;

        Dirty(ent.Owner, ent.Comp);

        _popup.PopupEntity(Loc.GetString("seraphim-implanter-start", ("target", target)), ent, PopupType.Medium);
        PlayWorkSound(ent);
    }

    private void OnImplantDoAfter(Entity<SeraphimImplanterComponent> ent, ref SeraphimImplantDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Cancelled)
        {
            if (ent.Comp.Active)
                AbortImplant(ent);
            return;
        }

        if (ent.Comp.Active)
            FinishImplant(ent);
    }

    private void AbortImplant(Entity<SeraphimImplanterComponent> ent)
    {
        ent.Comp.Active = false;
        ent.Comp.Target = null;
        if (ent.Comp.DoAfter is { } doAfter)
        {
            _doAfter.Cancel(doAfter);
            ent.Comp.DoAfter = null;
        }

        Dirty(ent.Owner, ent.Comp);
        _popup.PopupEntity(Loc.GetString("seraphim-implanter-abort"), ent, PopupType.Medium);
    }

    private void PlayWorkSound(Entity<SeraphimImplanterComponent> ent)
    {
        var stream = _audio.PlayPvs(ent.Comp.WorkSound, ent);
        ent.Comp.SoundStream = stream?.Entity;
    }

    private void FinishImplant(Entity<SeraphimImplanterComponent> ent)
    {
        var target = ent.Comp.Target;

        ent.Comp.Active = false;
        ent.Comp.Used = true;
        ent.Comp.Target = null;
        if (ent.Comp.DoAfter is { } doAfter)
        {
            _doAfter.Cancel(doAfter);
            ent.Comp.DoAfter = null;
        }

        Dirty(ent.Owner, ent.Comp);
        _appearance.SetData(ent.Owner, SeraphimImplanterVisuals.Empty, true);

        if (target is { } uid && Exists(uid))
        {
            _implant.AddImplant(uid, ent.Comp.Implant);
            AfterImplantEffects(uid);
        }
    }

    // Последствия установки
    private void AfterImplantEffects(EntityUid uid)
    {
        _vomit.Vomit(uid);
        _jitter.DoJitter(uid, TimeSpan.FromSeconds(8), true, 8f, 6f);

        EnsureComp<DizzyEffectComponent>(uid).Intensity = 80f;
        _status.TryAddStatusEffect<DrunkStatusEffectComponent>(uid, "Dizzy", TimeSpan.FromSeconds(30), true);

        _popup.PopupEntity(Loc.GetString("seraphim-implanter-done"), uid, uid, PopupType.Large);
    }

    // Периодические эффекты
    private void ImplantTickEffects(Entity<SeraphimImplanterComponent> ent, EntityUid target)
    {
        _chat.TryEmoteWithChat(target, "Scream", ChatTransmitRange.Normal, ignoreActionBlocker: true, forceEmote: true);
        _jitter.DoJitter(target, ent.Comp.EffectInterval + TimeSpan.FromSeconds(2), true, 6f, 5f);

        var roll = _random.Next(3);
        switch (roll)
        {
            case 0:
                _bloodstream.TryModifyBleedAmount(target, 3f);
                _popup.PopupEntity(Loc.GetString("seraphim-implanter-bleed"), target, PopupType.Medium);
                break;
            case 1:
                var solution = new Solution();
                solution.AddReagent(SpaceDrugs, FixedPoint2.New(3));
                _bloodstream.TryAddToBloodstream(target, solution);
                _popup.PopupEntity(Loc.GetString("seraphim-implanter-drugs"), target, PopupType.Medium);
                break;
            default:
                _bloodstream.TryModifyBleedAmount(target, 2f);
                _popup.PopupEntity(Loc.GetString("seraphim-implanter-pain"), target, PopupType.Medium);
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cur = _timing.CurTime;

        var prepared = EntityQueryEnumerator<SeraphimPreparedComponent>();
        while (prepared.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.ExpireTime)
            {
                _popup.PopupEntity(Loc.GetString("seraphim-prepared-expire"), uid, uid, PopupType.Small);
                RemComp<SeraphimPreparedComponent>(uid);
            }
        }

        var conc = EntityQueryEnumerator<SeraphimConcentratingComponent>();
        while (conc.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.EndTime
                || comp.Weapon is not { } weapon
                || !_hands.IsHolding(uid, weapon))
            {
                RemComp<SeraphimConcentratingComponent>(uid);
            }
        }

        var over = EntityQueryEnumerator<SeraphimOverdriveComponent>();
        while (over.MoveNext(out var uid, out var comp))
        {
            if (cur < comp.EndTime)
                continue;

            RemComp<SeraphimOverdriveComponent>(uid);
            _speed.RefreshMovementSpeedModifiers(uid);

            var exhausted = EnsureComp<SeraphimExhaustedComponent>(uid);
            exhausted.EndTime = cur + ExhaustedDuration;
            Dirty(uid, exhausted);
            _speed.RefreshMovementSpeedModifiers(uid);

            _stamina.TakeStaminaDamage(uid, 60f);
            _jitter.DoJitter(uid, TimeSpan.FromSeconds(10), true, 5f, 3f);
            EnsureComp<DizzyEffectComponent>(uid).Intensity = 60f;
            _status.TryAddStatusEffect<DrunkStatusEffectComponent>(uid, "Dizzy", TimeSpan.FromSeconds(20), true);
            _popup.PopupEntity(Loc.GetString("seraphim-overdrive-end"), uid, uid, PopupType.LargeCaution);
        }

        var exhaustedQuery = EntityQueryEnumerator<SeraphimExhaustedComponent>();
        while (exhaustedQuery.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.EndTime)
            {
                RemComp<SeraphimExhaustedComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
            }
        }

        var slowed = EntityQueryEnumerator<SeraphimSlowedComponent>();
        while (slowed.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.EndTime)
            {
                RemComp<SeraphimSlowedComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
            }
        }

        var implanter = EntityQueryEnumerator<SeraphimImplanterComponent>();
        while (implanter.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                continue;

            // Цель отстегнулась или удалилась
            if (comp.Target is not { } target
                || !Exists(target)
                || !TryComp<BuckleComponent>(target, out var buckle)
                || buckle.BuckledTo != uid)
            {
                AbortImplant((uid, comp));
                continue;
            }

            if (comp.SoundStream is not { } stream || !Exists(stream))
                PlayWorkSound((uid, comp));

            if (cur >= comp.EndTime)
            {
                FinishImplant((uid, comp));
                continue;
            }

            if (cur >= comp.NextEffectTime)
            {
                comp.NextEffectTime = cur + comp.EffectInterval;
                ImplantTickEffects((uid, comp), target);
            }
        }
    }
}
