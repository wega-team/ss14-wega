using System.Linq;
using System.Numerics;
using Content.Server.Implants;
using Content.Shared.Genetics;
using Content.Shared._Wega.Seraphim;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
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
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SubdermalImplantSystem _implant = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    private const string KneeType = "Knee";
    private const string FuckOffType = "FuckOff";

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
        SubscribeLocalEvent<SeraphimImplanterComponent, SimpleDoAfterEvent>(OnImplantDoAfter);
    }

    private void OnKneeAction(Entity<SeraphimImplantComponent> ent, ref SeraphimKneeActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var prep = EnsureComp<SeraphimPreparedComponent>(ent);
        prep.Type = KneeType;
        prep.ExpireTime = _timing.CurTime + prep.Duration;
        Dirty(ent.Owner, prep);

        _popup.PopupEntity(Loc.GetString("seraphim-knee-prepared"), ent, ent, PopupType.Medium);
        args.Handled = true;
    }

    private void OnFuckOffAction(Entity<SeraphimImplantComponent> ent, ref SeraphimFuckOffActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var prep = EnsureComp<SeraphimPreparedComponent>(ent);
        prep.Type = FuckOffType;
        prep.ExpireTime = _timing.CurTime + prep.Duration;
        Dirty(ent.Owner, prep);

        _popup.PopupEntity(Loc.GetString("seraphim-fuckoff-prepared"), ent, ent, PopupType.Medium);
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
        comp.EndTime = _timing.CurTime + comp.Duration;
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
        args.Handled = true;
    }

    private void OnOverdriveAction(Entity<SeraphimImplantComponent> ent, ref SeraphimOverdriveActionEvent args)
    {
        if (args.Handled || IsAbilityActive(ent))
            return;

        var comp = EnsureComp<SeraphimOverdriveComponent>(ent);
        comp.EndTime = _timing.CurTime + comp.Duration;
        Dirty(ent.Owner, comp);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        _entityEffects.ApplyEffects(ent, _proto.Index(comp.StartEffects).Effects);

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
                slow.EndTime = _timing.CurTime + slow.Duration;
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

    private void OnImplantDoAfter(Entity<SeraphimImplanterComponent> ent, ref SimpleDoAfterEvent args)
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
            AfterImplantEffects(ent, uid);
        }
    }

    // Последствия установки
    private void AfterImplantEffects(Entity<SeraphimImplanterComponent> ent, EntityUid uid)
    {
        EnsureComp<DizzyEffectComponent>(uid).Intensity = 80f;
        _entityEffects.ApplyEffects(uid, _proto.Index(ent.Comp.FinishEffects).Effects);
    }

    // Периодические эффекты
    private void ImplantTickEffects(Entity<SeraphimImplanterComponent> ent, EntityUid target)
    {
        var proto = _random.Next(3) switch
        {
            0 => ent.Comp.TickBleed,
            1 => ent.Comp.TickDrugs,
            _ => ent.Comp.TickPain,
        };

        _entityEffects.ApplyEffects(target, _proto.Index(proto).Effects);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cur = _timing.CurTime;

        UpdatePrepared(cur);
        UpdateConcentrating(cur);
        UpdateOverdrive(cur);
        UpdateExhausted(cur);
        UpdateSlowed(cur);
        UpdateImplanters(cur);
    }

    private void UpdatePrepared(TimeSpan cur)
    {
        var prepared = EntityQueryEnumerator<SeraphimPreparedComponent>();
        while (prepared.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.ExpireTime)
            {
                _popup.PopupEntity(Loc.GetString("seraphim-prepared-expire"), uid, uid, PopupType.Small);
                RemComp<SeraphimPreparedComponent>(uid);
            }
        }
    }

    private void UpdateConcentrating(TimeSpan cur)
    {
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
    }

    private void UpdateOverdrive(TimeSpan cur)
    {
        var over = EntityQueryEnumerator<SeraphimOverdriveComponent>();
        while (over.MoveNext(out var uid, out var comp))
        {
            if (cur < comp.EndTime)
                continue;

            EndOverdrive((uid, comp), cur);
        }
    }

    private void UpdateExhausted(TimeSpan cur)
    {
        var exhaustedQuery = EntityQueryEnumerator<SeraphimExhaustedComponent>();
        while (exhaustedQuery.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.EndTime)
            {
                RemComp<SeraphimExhaustedComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
            }
        }
    }

    private void UpdateSlowed(TimeSpan cur)
    {
        var slowed = EntityQueryEnumerator<SeraphimSlowedComponent>();
        while (slowed.MoveNext(out var uid, out var comp))
        {
            if (cur >= comp.EndTime)
            {
                RemComp<SeraphimSlowedComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
            }
        }
    }

    private void UpdateImplanters(TimeSpan cur)
    {
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

    private void EndOverdrive(Entity<SeraphimOverdriveComponent> ent, TimeSpan cur)
    {
        // Читаем эффекты до удаления компонента
        var effects = _proto.Index(ent.Comp.EndEffects).Effects;

        RemComp<SeraphimOverdriveComponent>(ent);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        var exhausted = EnsureComp<SeraphimExhaustedComponent>(ent);
        exhausted.EndTime = cur + exhausted.Duration;
        Dirty(ent, exhausted);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        EnsureComp<DizzyEffectComponent>(ent).Intensity = 60f;
        _entityEffects.ApplyEffects(ent, effects);
    }
}
