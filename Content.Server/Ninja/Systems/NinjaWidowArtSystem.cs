using System.Linq;
using System.Numerics;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Robust.Shared.Map;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Server._Wega.Chemistry.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.MouseRotator;
using Content.Server.Grab.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.Surgery;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Implements the Creeping Widow martial art for the space ninja:
/// - Concentration HUD icon, 15 s cooldown after a successful combo
/// - Projectile reflection (50 % chance) when holding a katana and concentrated
/// - Energy Tornado  H/D/H/H — push all nearby mobs within 1.5 tiles; ninja spins 1.5 s
/// - Palm Strike     H/H/H   — knock back target; victim can't stand 3 s
/// - Wrist Lock      Help/H  — force target to drop held item
/// - Neck Slice      D/H/D   — instant kill downed target (katana in off-hand; long cooldown)
/// - Neck Strike             — mutes target 15 s + damage when grabbed from behind while concentrated
/// </summary>
public sealed class NinjaWidowArtSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem           _alerts       = default!;
    [Dependency] private readonly SharedAudioSystem      _audio        = default!;
    [Dependency] private readonly DamageableSystem       _damage       = default!;
    [Dependency] private readonly SurgerySystem          _surgery      = default!;
    [Dependency] private readonly EntityLookupSystem     _lookup       = default!;
    [Dependency] private readonly SharedHandsSystem      _hands        = default!;
    [Dependency] private readonly IGameTiming            _timing       = default!;
    [Dependency] private readonly IRobustRandom          _random       = default!;
    [Dependency] private readonly SharedPopupSystem      _popup        = default!;
    [Dependency] private readonly SharedPhysicsSystem    _physics      = default!;
    [Dependency] private readonly SharedStunSystem       _stun         = default!;
    [Dependency] private readonly StatusEffectsSystem     _statusEffect = default!;
    [Dependency] private readonly ThrowingSystem         _throwing     = default!;
    [Dependency] private readonly SharedTransformSystem  _xform        = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode   = default!;

    private const float PushSpeed        = 8f;
    private const float TornadoSpeed     = 10f;
    private const float TornadoRange     = 1.5f;
    private const float TornadoSpinSpeed = 20f; // degrees-per-second multiplier

    // Combos checked longest-first to avoid prefix conflicts
    private static readonly (WidowInput[] Pattern, string Name)[] Combos =
    {
        (new[] { WidowInput.Harm, WidowInput.Disarm, WidowInput.Harm, WidowInput.Harm }, "Tornado"),
        (new[] { WidowInput.Disarm, WidowInput.Harm, WidowInput.Disarm },                "NeckSlice"),
        (new[] { WidowInput.Harm, WidowInput.Harm, WidowInput.Harm },                    "PalmStrike"),
        (new[] { WidowInput.Help, WidowInput.Harm },                                     "WristLock"),
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaWidowArtComponent, ClothingGotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<NinjaWidowArtComponent, ClothingGotUnequippedEvent>(OnSuitUnequipped);

        SubscribeLocalEvent<NinjaConcentrationComponent, ProjectileReflectAttemptEvent>(OnProjectileHit);

        // Harm input: only unarmed strikes (user IS weapon) count toward combos
        SubscribeLocalEvent<NinjaConcentrationComponent, MeleeHitEvent>(OnUnarmedHit);
        // Disarm / Help inputs: raised on the target entity
        SubscribeLocalEvent<BodyComponent, DisarmAttemptEvent>(OnTargetDisarmed);
        SubscribeLocalEvent<BodyComponent, InteractHandEvent>(OnTargetHelped);

        SubscribeLocalEvent<NinjaConcentrationComponent, GrabStartedEvent>(OnGrabStarted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NinjaConcentrationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ComboHistory.Count > 0 && now - comp.LastInputTime > comp.ComboResetTime)
                ClearCombo(uid, comp);

            // Transition cooldown → active exactly once when the timer expires
            if (comp.AlertShowingCooldown && now >= comp.CooldownEnd)
            {
                comp.AlertShowingCooldown = false;
                _alerts.ShowAlert(uid, comp.ConcentrationAlert, severity: (short)1);
            }

            // Stop tornado spin once the duration elapses; restore FixedRotation
            if (comp.TornadoSpinUntil > TimeSpan.Zero)
            {
                if (now >= comp.TornadoSpinUntil)
                {
                    comp.TornadoSpinUntil = TimeSpan.Zero;
                    if (HasComp<CombatModeComponent>(uid))
                        EnsureComp<MouseRotatorComponent>(uid);
                }
                else
                {
                    var xform = Transform(uid);
                    _xform.SetLocalRotation(uid, xform.LocalRotation + Angle.FromDegrees(TornadoSpinSpeed * 60f * frameTime), xform);
                }
            }
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called from NinjaSuitSystem when the suit is already equipped and the player picks this art via SpiderOS.
    /// </summary>
    public void GrantWidowArt(EntityUid suitUid, EntityUid user)
    {
        var widowComp = EnsureComp<NinjaWidowArtComponent>(suitUid);
        widowComp.WearerEntity = user;
        ActivateConcentration(user);
    }

    // ── Suit equip / unequip ───────────────────────────────────────────────────

    private void OnSuitEquipped(Entity<NinjaWidowArtComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.WearerEntity = args.Wearer;
        ActivateConcentration(args.Wearer);
    }

    private void OnSuitUnequipped(Entity<NinjaWidowArtComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.WearerEntity = null;
        if (TryComp<NinjaConcentrationComponent>(args.Wearer, out var comp))
            _alerts.ClearAlert(args.Wearer, comp.ConcentrationAlert);
        RemCompDeferred<NinjaConcentrationComponent>(args.Wearer);
    }

    private void ActivateConcentration(EntityUid user)
    {
        var comp = EnsureComp<NinjaConcentrationComponent>(user);
        comp.AlertShowingCooldown = false;
        _alerts.ShowAlert(user, comp.ConcentrationAlert, severity: (short)1);
    }

    // ── Combo input recording ──────────────────────────────────────────────────

    private void OnUnarmedHit(Entity<NinjaConcentrationComponent> attacker, ref MeleeHitEvent args)
    {
        // Unarmed: the ninja entity is both user and weapon; only fire for actual hits
        if (attacker.Owner != args.User || !args.IsHit || args.HitEntities.Count == 0)
            return;
        RecordInput(attacker.Owner, args.HitEntities[0], WidowInput.Harm);
    }

    private void OnTargetDisarmed(Entity<BodyComponent> target, ref DisarmAttemptEvent args)
    {
        RecordInput(args.DisarmerUid, target.Owner, WidowInput.Disarm);
    }

    private void OnTargetHelped(Entity<BodyComponent> target, ref InteractHandEvent args)
    {
        if (_combatMode.IsInCombatMode(args.User))
            return;
        RecordInput(args.User, target.Owner, WidowInput.Help);
    }

    private void RecordInput(EntityUid user, EntityUid target, WidowInput input)
    {
        if (!TryComp<NinjaConcentrationComponent>(user, out var comp))
            return;

        // Combos only work on creatures (mobs), not items or structures — includes critted mobs
        if (!HasComp<MobStateComponent>(target))
            return;

        var now = _timing.CurTime;

        if (now < comp.CooldownEnd)
            return; // on cooldown, art unavailable

        if (comp.ComboTarget != target)
        {
            ClearCombo(user, comp);
            comp.ComboTarget = target;
        }

        comp.ComboHistory.Add(input);
        comp.LastInputTime = now;

        const int MaxHistory = 4;
        if (comp.ComboHistory.Count > MaxHistory)
            comp.ComboHistory.RemoveAt(0);

        Dirty(user, comp);
        CheckCombos(user, target, comp, now);
    }

    private void CheckCombos(EntityUid user, EntityUid target, NinjaConcentrationComponent comp, TimeSpan now)
    {
        foreach (var (pattern, name) in Combos)
        {
            if (!HistoryEndsWith(comp.ComboHistory, pattern))
                continue;

            ExecuteCombo(name, user, target, comp, now);
            return;
        }
    }

    private static bool HistoryEndsWith(List<WidowInput> history, WidowInput[] pattern)
    {
        if (history.Count < pattern.Length)
            return false;

        var offset = history.Count - pattern.Length;
        for (var i = 0; i < pattern.Length; i++)
        {
            if (history[offset + i] != pattern[i])
                return false;
        }

        return true;
    }

    // ── Combo execution ────────────────────────────────────────────────────────

    private void ExecuteCombo(string name, EntityUid user, EntityUid target, NinjaConcentrationComponent comp, TimeSpan now)
    {
        switch (name)
        {
            case "Tornado":
                ExecuteTornado(user, target, comp, now);
                break;
            case "PalmStrike":
                ExecutePalmStrike(user, target);
                break;
            case "WristLock":
                ExecuteWristLock(user, target);
                break;
            case "NeckSlice":
                ExecuteNeckSlice(user, target, comp, now);
                return; // sets its own longer cooldown
        }

        SetCooldown(user, comp, comp.ConcentrationCooldown, now);
    }

    private void ExecuteTornado(EntityUid user, EntityUid target, NinjaConcentrationComponent comp, TimeSpan now)
    {
        var userCoords = _xform.GetMapCoordinates(user);

        // Custom mob-only radial push within 1.5 tiles
        // (RepulseAttractSystem uses SingularityLayer which skips mobs)
        foreach (var (mob, _) in _lookup.GetEntitiesInRange<MobStateComponent>(userCoords, TornadoRange))
        {
            if (mob == user)
                continue;
            PushAway(user, mob);
            TryComp<CrawlerComponent>(mob, out var crawler);
            _stun.TryKnockdown((mob, crawler), TimeSpan.FromSeconds(2));
        }

        // Ninja spins visually for 1.5 s
        // FixedRotation defaults to true for mobs; must disable it first to allow rotation
        RemComp<MouseRotatorComponent>(user);
        comp.TornadoSpinUntil = now + TimeSpan.FromSeconds(0.75);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), user);
        _popup.PopupEntity(Loc.GetString("widow-art-tornado"), user, user);
        _popup.PopupEntity(Loc.GetString("widow-art-tornado-target"), target, Filter.PvsExcept(user), true);
    }

    private void ExecutePalmStrike(EntityUid user, EntityUid target)
    {
        PushAway(user, target);
        // Victim cannot stand for 3 seconds
        TryComp<CrawlerComponent>(target, out var crawler);
        _stun.TryKnockdown((target, crawler), TimeSpan.FromSeconds(3));
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg"), target);
        _popup.PopupEntity(Loc.GetString("widow-art-palm-strike"), user, user);
    }

    private void ExecuteWristLock(EntityUid user, EntityUid target)
    {
        var dropped = false;
        // Collect held items first — TryDrop mutates the hands, so we can't drop mid-enumeration.
        var heldItems = new List<EntityUid>();
        foreach (var handId in _hands.EnumerateHands(target))
        {
            if (_hands.TryGetHeldItem(target, handId, out var held))
                heldItems.Add(held.Value);
        }

        // Guaranteed drop from every hand.
        foreach (var item in heldItems)
        {
            if (_hands.TryDrop(target, item, checkActionBlocker: false))
                dropped = true;
        }

        if (!dropped)
            return;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Emotes/snap1.ogg"), target);
        _popup.PopupEntity(Loc.GetString("widow-art-wrist-lock"), user, user);
        _popup.PopupEntity(Loc.GetString("widow-art-wrist-lock-target"), target, Filter.PvsExcept(user), true);
    }

    private void ExecuteNeckSlice(EntityUid user, EntityUid target, NinjaConcentrationComponent comp, TimeSpan now)
    {
        if (!IsDown(target))
        {
            _popup.PopupEntity(Loc.GetString("widow-art-neck-slice-fail-standing"), user, user);
            return;
        }

        if (!HasKatanaInInactiveHand(user))
        {
            _popup.PopupEntity(Loc.GetString("widow-art-neck-slice-fail-no-katana"), user, user);
            return;
        }

        _surgery.TryDecapitate(target, user);

        _popup.PopupEntity(Loc.GetString("widow-art-neck-slice"), user, user);
        _popup.PopupEntity(Loc.GetString("widow-art-neck-slice-target"), target, Filter.PvsExcept(user), true);

        SetCooldown(user, comp, comp.NeckSliceCooldown, now);
    }

    // ── Projectile reflection ──────────────────────────────────────────────────

    private void OnProjectileHit(Entity<NinjaConcentrationComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.CooldownEnd)
            return;

        if (!HasKatanaInHand(ent.Owner))
            return;

        if (!_random.Prob(0.5f))
            return;

        if (TryComp<PhysicsComponent>(args.ProjUid, out var body))
        {
            var vel = _physics.GetMapLinearVelocity(args.ProjUid, component: body);
            _physics.SetLinearVelocity(args.ProjUid, -vel, body: body);
        }

        args.Cancelled = true;
        _popup.PopupEntity(Loc.GetString("widow-art-reflect"), ent.Owner, ent.Owner);
    }

    // ── Neck Strike (grab from behind → mute + damage + cooldown) ─────────────

    private void OnGrabStarted(Entity<NinjaConcentrationComponent> grabber, ref GrabStartedEvent args)
    {
        if (grabber.Owner != args.Grabber)
            return;

        RecordInput(grabber.Owner, args.Victim, WidowInput.Grab);

        var now = _timing.CurTime;
        if (now < grabber.Comp.CooldownEnd)
            return;

        if (!IsBehindTarget(grabber.Owner, args.Victim))
            return;

        // 15 s mute + asphyxiation/blunt damage
        _statusEffect.TryAddStatusEffect<MutedComponent>(args.Victim, "Muted", TimeSpan.FromSeconds(15f), true);
        var strikeDamage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 20 }, { "Blunt", 5 } } };
        _damage.TryChangeDamage(args.Victim, strikeDamage, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg"), args.Victim);
        SetCooldown(grabber.Owner, grabber.Comp, grabber.Comp.ConcentrationCooldown, now);
        _popup.PopupEntity(Loc.GetString("widow-art-neck-strike"), grabber.Owner, grabber.Owner);
        _popup.PopupEntity(Loc.GetString("widow-art-neck-strike-target"), args.Victim, Filter.PvsExcept(grabber.Owner), true);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void PushAway(EntityUid from, EntityUid target)
    {
        if (HasComp<ChiurizinStasisComponent>(target))
            return;

        var fromPos   = _xform.GetWorldPosition(from);
        var targetPos = _xform.GetWorldPosition(target);
        var dir       = targetPos - fromPos;

        if (dir == Vector2.Zero)
            dir = Vector2.UnitY;
        else
            dir = Vector2.Normalize(dir);

        if (TryComp<PhysicsComponent>(target, out var physics) &&
                 (physics.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0)
        {
            _physics.SetLinearVelocity(target, dir * PushSpeed, body: physics);
        }
        else
        {
            _throwing.TryThrow(target, dir * PushSpeed, PushSpeed, from, pushbackRatio: 0f, recoil: false);
        }
    }

    private bool IsDown(EntityUid uid)
        => TryComp<StandingStateComponent>(uid, out var s) && !s.Standing;

    private bool HasKatanaInHand(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (HasComp<EnergyKatanaComponent>(held))
                return true;
        }
        return false;
    }

    private bool HasKatanaInInactiveHand(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        var activeId = hands.ActiveHandId;
        foreach (var handId in _hands.EnumerateHands((uid, hands)))
        {
            if (handId == activeId)
                continue;

            if (_hands.TryGetHeldItem((uid, hands), handId, out var held)
                && HasComp<EnergyKatanaComponent>(held.Value))
                return true;
        }
        return false;
    }

    private bool IsBehindTarget(EntityUid attacker, EntityUid target)
    {
        var attackerPos  = _xform.GetWorldPosition(attacker);
        var targetPos    = _xform.GetWorldPosition(target);
        var targetFacing = _xform.GetWorldRotation(target).ToWorldVec();

        var toAttacker = attackerPos - targetPos;
        if (toAttacker == Vector2.Zero)
            return false;

        return Vector2.Dot(targetFacing, Vector2.Normalize(toAttacker)) < 0f;
    }

    private void SetCooldown(EntityUid uid, NinjaConcentrationComponent comp, TimeSpan duration, TimeSpan now)
    {
        comp.CooldownEnd = now + duration;
        comp.AlertShowingCooldown = true;
        ClearCombo(uid, comp);
        _alerts.ShowAlert(uid, comp.ConcentrationAlert, severity: (short)0,
            cooldown: (now, comp.CooldownEnd), showCooldown: true);
    }

    private void ClearCombo(EntityUid uid, NinjaConcentrationComponent comp)
    {
        comp.ComboHistory.Clear();
        comp.ComboTarget = null;
        Dirty(uid, comp);
    }
}
