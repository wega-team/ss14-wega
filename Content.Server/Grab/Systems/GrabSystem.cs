using Content.Server.Grab.Components;
using Content.Server.Grab.Events;
using Content.Server.Inventory;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Grab.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Grab.Systems;

/// <summary>
/// Grab system: pressing the pull key in combat mode initiates a grab instead of a pull.
/// Victims cannot move while grabbed. They can escape by pressing a movement key,
/// with decreasing success chances as the grab escalates through three phases.
/// Phase 3 deals continuous asphyxiation damage and blocks both of the grabber's hands.
/// </summary>
public sealed partial class GrabSystem : EntitySystem
{
    private static readonly float[] EscapeChances = { 1.0f, 0.25f, 0.05f };

    private const float MaxGrabDistance = 1.5f;
    private const float EscapeCooldown = 0.5f;
    private const float PhaseChangeCooldown = 2.0f;
    private const float Phase3DamagePerSecond = 2.5f;
    private const float GrabThrowSpeed = 10f;
    private const float GrabThrownDuration = 1.5f;
    private const float WallKnockdownSeconds = 3f;

    private static readonly SoundSpecifier GrabSound =
        new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [Dependency] private ActionBlockerSystem    _actionBlocker = default!;
    [Dependency] private DamageableSystem       _damage        = default!;
    [Dependency] private SharedCombatModeSystem _combatMode    = default!;
    [Dependency] private SharedPopupSystem      _popup         = default!;
    [Dependency] private SharedTransformSystem  _xform         = default!;
    [Dependency] private IRobustRandom          _random        = default!;
    [Dependency] private IGameTiming            _timing        = default!;
    [Dependency] private SharedAudioSystem             _audio       = default!;
    [Dependency] private PullingSystem                 _pulling     = default!;
    [Dependency] private SharedColorFlashEffectSystem  _color       = default!;
    [Dependency] private VirtualItemSystem             _virtualItem = default!;
    [Dependency] private AlertsSystem                  _alerts      = default!;
    [Dependency] private SharedStutteringSystem        _stuttering    = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedPhysicsSystem         _physics       = default!;
    [Dependency] private SharedStunSystem            _stun          = default!;
    [Dependency] private ThrowingSystem              _throwing      = default!;

    // Prevents our BeingPulledAttemptEvent handler from intercepting the pull we start in StartGrab.
    private bool _initiatingGrabPull;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent,  BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        SubscribeLocalEvent<GrabbedComponent,   UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<GrabbedComponent,   MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<GrabbedComponent,   AttemptStopPullingEvent>(OnGrabbedAttemptStopPull);
        SubscribeLocalEvent<GrabbedComponent,   StopBeingPulledAlertEvent>(OnGrabbedStopBeingPulledAlert);
        SubscribeLocalEvent<GrabbingComponent,  PullStoppedMessage>(OnGrabberPullStopped);
        SubscribeLocalEvent<GrabbingComponent,  DownedEvent>(OnGrabberDowned);
        SubscribeLocalEvent<GrabbingComponent,  MobStateChangedEvent>(OnGrabberMobStateChanged);
        SubscribeLocalEvent<GrabbingComponent,  DisarmAttemptEvent>(OnGrabberDisarmAttempt);
        SubscribeLocalEvent<GrabbingComponent,  VirtualItemDeletedEvent>(OnGrabberVirtualItemDeleted);
        SubscribeLocalEvent<GrabThrownComponent, StartCollideEvent>(OnGrabThrownCollide);
        SubscribeLocalEvent<GrabbedComponent,   ComponentShutdown>(OnGrabbedShutdown);
        SubscribeLocalEvent<GrabbingComponent,  ComponentShutdown>(OnGrabbingShutdown);
    }

    private void OnBeingPulledAttempt(EntityUid uid, MobStateComponent _, BeingPulledAttemptEvent args)
    {
        if (!_combatMode.IsInCombatMode(args.Puller))
            return;

        // Let through the pull that StartGrab initiates internally.
        if (_initiatingGrabPull)
            return;

        // Always cancel normal pull in combat mode — grab instead
        args.Cancel();

        var now = _timing.CurTime;

        // Already grabbed by the same grabber → escalate phase and restart pull
        if (TryComp<GrabbedComponent>(uid, out var grabbed) && grabbed.Grabber == args.Puller)
        {
            if (TryComp<GrabbingComponent>(args.Puller, out var grabbing))
                TryEscalatePhase(args.Puller, uid, grabbed, grabbing, now);

            _initiatingGrabPull = true;
            _pulling.TryStartPull(args.Puller, uid);
            _initiatingGrabPull = false;
            return;
        }

        // Already grabbed by someone else — do nothing
        if (HasComp<GrabbedComponent>(uid))
            return;

        StartGrab(args.Puller, uid, now);
    }

    private void StartGrab(EntityUid grabber, EntityUid victim, TimeSpan now)
    {
        var grabbed = EnsureComp<GrabbedComponent>(victim);
        grabbed.Grabber = grabber;
        grabbed.Phase = 1;

        var grabbing = EnsureComp<GrabbingComponent>(grabber);
        grabbing.Victim = victim;
        grabbing.NextPhaseChangeTime = now + TimeSpan.FromSeconds(PhaseChangeCooldown);

        _actionBlocker.UpdateCanMove(victim);

        _color.RaiseEffect(new Color(1f, 0.8f, 0f), new List<EntityUid>() { victim },
            Filter.Pvs(victim, entityManager: EntityManager));

        // Fire before TryStartPull so IsBehindTarget checks run before entity rotations update.
        RaiseLocalEvent(grabber, new GrabStartedEvent { Grabber = grabber, Victim = victim });

        UpdateGrabSpeed(grabber, 1);

        // Start dragging the victim; bypass our own BeingPulledAttemptEvent interception.
        _initiatingGrabPull = true;
        _pulling.TryStartPull(grabber, victim);
        _initiatingGrabPull = false;

        _audio.PlayPvs(GrabSound, grabber, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));
        _popup.PopupEntity(Loc.GetString("grab-start-grabber"), grabber, grabber);
        _popup.PopupEntity(Loc.GetString("grab-start-victim"), victim, victim);
    }

    // Returns true if the pull stop should be cancelled (escalating or on cooldown),
    // false if it should proceed (already at max phase — let the grabber release).
    private bool TryEscalatePhase(EntityUid grabber, EntityUid victim,
        GrabbedComponent grabbed, GrabbingComponent grabbing, TimeSpan now)
    {
        if (now < grabbing.NextPhaseChangeTime)
        {
            _popup.PopupEntity(Loc.GetString("grab-escalate-cooldown"), grabber, grabber);
            return true;
        }

        if (grabbed.Phase >= 3)
        {
            _popup.PopupEntity(Loc.GetString("grab-max-phase"), grabber, grabber);
            return false;
        }

        grabbed.Phase++;
        grabbing.NextPhaseChangeTime = now + TimeSpan.FromSeconds(PhaseChangeCooldown);

        _color.RaiseEffect(new Color(1f, 0.8f, 0f), new List<EntityUid>() { victim },
            Filter.Pvs(victim, entityManager: EntityManager));
        _audio.PlayPvs(GrabSound, grabber, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        _popup.PopupEntity(Loc.GetString("grab-phase-escalated-grabber", ("phase", grabbed.Phase)), grabber, grabber);
        _popup.PopupEntity(Loc.GetString("grab-phase-escalated-victim",  ("phase", grabbed.Phase)), victim, victim);

        UpdateGrabSpeed(grabber, grabbed.Phase);

        if (grabbed.Phase == 3)
        {
            // Block both hands — grabber is now choking with both hands.
            _virtualItem.TrySpawnVirtualItemInHand(victim, grabber);
            _virtualItem.TrySpawnVirtualItemInHand(victim, grabber);

            // Apply strangulation effects to the victim.
            var dropEv = new DropHandItemsEvent();
            RaiseLocalEvent(victim, ref dropEv);
            _alerts.ShowAlert(victim, "StrangledAlert");
            _combatMode.SetDisarmFailChance(victim, 0.9f);
            grabbed.Phase3Applied = true;
        }

        return true;
    }

    private void OnUpdateCanMove(EntityUid uid, GrabbedComponent component, UpdateCanMoveEvent args)
    {
        // Phase 0 means ReleaseGrab has already fired — movement is unblocked.
        if (!component.Running || component.Phase == 0)
            return;

        args.Cancel();
    }

    private void OnMoveInput(EntityUid uid, GrabbedComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement || component.Phase == 0)
            return;

        var now = _timing.CurTime;
        if (now < component.NextEscapeAttemptTime)
            return;

        component.NextEscapeAttemptTime = now + TimeSpan.FromSeconds(EscapeCooldown);

        var chance = EscapeChances[component.Phase - 1];
        var grabber = component.Grabber;

        if (_random.Prob(chance))
        {
            _popup.PopupEntity(Loc.GetString("grab-escaped-grabber"), grabber, grabber);
            _popup.PopupEntity(Loc.GetString("grab-escaped-victim"), uid, uid);
            ReleaseGrab(grabber);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("grab-escape-failed"), uid, uid);
        }
    }

    // Fires when the victim clicks their "Pulled" status alert — treated as an escape attempt.
    // Always intercepts (sets Handled=true) to prevent the default free pull-stop.
    private void OnGrabbedStopBeingPulledAlert(EntityUid uid, GrabbedComponent grabbed, ref StopBeingPulledAlertEvent args)
    {
        args.Handled = true;

        if (grabbed.Phase == 0)
            return;

        var now = _timing.CurTime;
        if (now < grabbed.NextEscapeAttemptTime)
            return;

        grabbed.NextEscapeAttemptTime = now + TimeSpan.FromSeconds(EscapeCooldown);

        var grabber = grabbed.Grabber;
        if (_random.Prob(EscapeChances[grabbed.Phase - 1]))
        {
            _popup.PopupEntity(Loc.GetString("grab-escaped-grabber"), grabber, grabber);
            _popup.PopupEntity(Loc.GetString("grab-escaped-victim"), uid, uid);
            ReleaseGrab(grabber);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("grab-escape-failed"), uid, uid);
        }
    }

    // Fires when the grabber presses the toggle-pull key while the grab is active.
    // In combat mode: escalate if possible, otherwise release at max phase.
    // Outside combat mode: let the pull stop proceed normally (ReleaseGrab via OnGrabberPullStopped).
    private void OnGrabbedAttemptStopPull(EntityUid uid, GrabbedComponent grabbed, ref AttemptStopPullingEvent args)
    {
        if (args.User != grabbed.Grabber)
            return;
        if (!_combatMode.IsInCombatMode(grabbed.Grabber))
            return;
        if (!TryComp<GrabbingComponent>(grabbed.Grabber, out var grabbing))
            return;

        var cancelStop = TryEscalatePhase(grabbed.Grabber, uid, grabbed, grabbing, _timing.CurTime);
        if (cancelStop)
        {
            args.Cancelled = true;
        }
        else
        {
            // At max phase — allow the pull to stop and release the grab.
            grabbing.PendingUserRelease = true;
        }
    }

    // Fires when the pull stops for reasons outside the toggle-pull key:
    // physics joint break, container insert, buckle, etc.
    // PendingUserRelease: grabber intentionally released at max phase → release grab.
    // In combat mode otherwise: restart the pull to maintain the grab.
    // Outside combat mode: release so the victim is not left movement-locked.
    private void OnGrabberPullStopped(EntityUid uid, GrabbingComponent grabbing, PullStoppedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        if (grabbing.LifeStage >= ComponentLifeStage.Stopping)
            return;

        if (grabbing.PendingUserRelease)
        {
            ReleaseGrab(uid);
            return;
        }

        if (!_combatMode.IsInCombatMode(uid))
        {
            ReleaseGrab(uid);
            return;
        }

        // Defer restart to avoid re-entrant TryStartPull inside StopPulling's call chain.
        grabbing.NeedsPullRestart = true;
    }

    // Prevents the grabbed victim from disarming their grabber to escape.
    private static void OnGrabberDisarmAttempt(EntityUid _, GrabbingComponent grabbing, ref DisarmAttemptEvent args)
    {
        if (args.DisarmerUid == grabbing.Victim)
            args.Cancelled = true;
    }

    // If a virtual item tied to the victim is removed externally (e.g. forced out of hand),
    // release the grab — the grabber can no longer maintain a two-handed choke.
    private void OnGrabberVirtualItemDeleted(EntityUid uid, GrabbingComponent grabbing, VirtualItemDeletedEvent args)
    {
        if (grabbing.IsReleasing)
            return;
        if (args.BlockingEntity != grabbing.Victim)
            return;
        ReleaseGrab(uid);
    }

    private void OnGrabberDowned(EntityUid uid, GrabbingComponent component, DownedEvent args)
    {
        ReleaseGrab(uid);
    }

    private void OnGrabberMobStateChanged(EntityUid uid, GrabbingComponent component, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            ReleaseGrab(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Process deferred pull restarts (flagged by OnGrabberPullStopped to avoid re-entrant TryStartPull).
        List<EntityUid>? toRelease = null;
        var grabberQuery = EntityQueryEnumerator<GrabbingComponent>();
        while (grabberQuery.MoveNext(out var grabber, out var grabbing))
        {
            if (!grabbing.NeedsPullRestart)
                continue;
            grabbing.NeedsPullRestart = false;
            _initiatingGrabPull = true;
            var started = _pulling.TryStartPull(grabber, grabbing.Victim);
            _initiatingGrabPull = false;
            if (!started)
            {
                toRelease ??= new();
                toRelease.Add(grabber);
            }
        }
        // Release after loop — modifying GrabbingComponent storage while iterating it causes IndexOutOfRangeException.
        if (toRelease != null)
            foreach (var g in toRelease)
                ReleaseGrab(g);

        // Clean up expired GrabThrownComponent
        var thrownQuery = EntityQueryEnumerator<GrabThrownComponent>();
        while (thrownQuery.MoveNext(out var thrownUid, out var thrown))
        {
            if (now >= thrown.ExpiresAt)
                RemCompDeferred<GrabThrownComponent>(thrownUid);
        }

        var query = EntityQueryEnumerator<GrabbedComponent>();

        while (query.MoveNext(out var victim, out var grabbed))
        {
            // Phase 0 means ReleaseGrab already fired but deferred removal hasn't run yet — skip.
            if (grabbed.Phase == 0)
                continue;

            // Release if grabber is gone
            if (!Exists(grabbed.Grabber))
            {
                RemCompDeferred<GrabbedComponent>(victim);
                continue;
            }

            // Release if too far apart
            var grabberPos = _xform.GetWorldPosition(grabbed.Grabber);
            var victimPos  = _xform.GetWorldPosition(victim);
            if ((grabberPos - victimPos).Length() > MaxGrabDistance)
            {
                ReleaseGrab(grabbed.Grabber);
                continue;
            }

            // Phase 3: deal asphyxiation damage every second and keep victim stuttering.
            if (grabbed.Phase == 3)
            {
                if (TryComp<MobStateComponent>(victim, out var mobState) && mobState.CurrentState == MobState.Dead)
                    continue;

                _stuttering.DoStutter(victim, TimeSpan.FromSeconds(5), refresh: true);

                if (now >= grabbed.NextDamageTime)
                {
                    grabbed.NextDamageTime = now + TimeSpan.FromSeconds(1.0);
                    var dmg = new DamageSpecifier { DamageDict = { { "Asphyxiation", Phase3DamagePerSecond } } };
                    _damage.TryChangeDamage(victim, dmg, true);
                }
            }
        }
    }

    /// <summary>
    /// Releases the grab initiated by <paramref name="grabber"/>.
    /// Uses deferred removal — safe to call from within event handlers.
    /// </summary>
    public void ReleaseGrab(EntityUid grabber)
    {
        if (!TryComp<GrabbingComponent>(grabber, out var grabbing))
            return;

        // IsReleasing is set by us before any work begins — stops re-entrant calls from
        // DeleteInHandsMatching → VirtualItemDeletedEvent → OnGrabberVirtualItemDeleted chains.
        if (grabbing.IsReleasing)
            return;
        grabbing.IsReleasing = true;
        UpdateGrabSpeed(grabber, 0);

        var victim = grabbing.Victim;

        if (TryComp<GrabbedComponent>(victim, out var grabbed))
        {
            var wasPhase3 = grabbed.Phase3Applied;
            grabbed.Phase = 0;
            _actionBlocker.UpdateCanMove(victim);

            if (wasPhase3)
            {
                _alerts.ClearAlert(victim, "StrangledAlert");
                _combatMode.SetDisarmFailChance(victim, 0.75f);
                _stuttering.DoRemoveStutterTime(victim, TimeSpan.FromSeconds(5));
            }
        }

        // LifeStage >= Stopping means we arrived here via an external cascade
        // (e.g. GrabbedComponent removed externally → OnGrabbedShutdown → OnGrabberVirtualItemDeleted).
        // RemCompDeferred was already called by the cascade; calling it again would hit _deleteSet.Add → assert.
        if (grabbing.LifeStage < ComponentLifeStage.Stopping)
            RemCompDeferred<GrabbingComponent>(grabber);

        // Clean up any phase-3 virtual items still in the grabber's hands.
        // IsReleasing = true, so OnGrabberVirtualItemDeleted won't recurse back.
        _virtualItem.DeleteInHandsMatching(grabber, victim);
    }

    /// <summary>
    /// Throws the grabbed victim toward <paramref name="targetCoords"/>.
    /// Returns true if the throw happened (phase >= 2).
    /// </summary>
    public bool TryThrowGrabbed(EntityUid grabber, EntityCoordinates targetCoords)
    {
        if (!TryComp<GrabbingComponent>(grabber, out var grabbing))
            return false;

        var victim = grabbing.Victim;
        if (!TryComp<GrabbedComponent>(victim, out var grabbed) || grabbed.Phase < 2)
            return false;

        var grabberPos = _xform.GetWorldPosition(grabber);
        var targetPos = _xform.ToMapCoordinates(targetCoords).Position;
        var direction = targetPos - grabberPos;

        if (direction == System.Numerics.Vector2.Zero)
            direction = _xform.GetWorldPosition(victim) - grabberPos;
        if (direction == System.Numerics.Vector2.Zero)
            return false;

        var length = direction.Length();
        var distance = MathF.Min(length, 6f);
        direction = direction / length * distance;

        ReleaseGrab(grabber);

        _throwing.TryThrow(victim, direction, GrabThrowSpeed, grabber, recoil: false, playSound: false, doSpin: false);

        var thrown = EnsureComp<GrabThrownComponent>(victim);
        thrown.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(GrabThrownDuration);

        _audio.PlayPvs(GrabSound, grabber, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));
        _popup.PopupEntity(Loc.GetString("grab-throw-victim"), victim, victim);
        return true;
    }

    private void OnGrabThrownCollide(EntityUid uid, GrabThrownComponent comp, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard || args.OtherBody.BodyType != Robust.Shared.Physics.BodyType.Static)
            return;

        RemCompDeferred<GrabThrownComponent>(uid);

        if (TryComp<PhysicsComponent>(uid, out var body))
            _physics.SetLinearVelocity(uid, System.Numerics.Vector2.Zero, body: body);

        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(WallKnockdownSeconds), true);
        _popup.PopupEntity(Loc.GetString("grab-throw-wall-hit"), uid, uid);
    }

    private void UpdateGrabSpeed(EntityUid grabber, int phase)
    {
        if (!TryComp<PullerComponent>(grabber, out var puller))
            return;

        var mod = phase switch
        {
            1 => 1.0f,
            2 => 0.7f,
            3 => 0.4f,
            _ => (float?) null,
        };

        puller.GrabWalkMod = mod;
        puller.GrabSprintMod = mod;
        Dirty(grabber, puller);
        _movementSpeed.RefreshMovementSpeedModifiers(grabber);
    }

    private void OnGrabbedShutdown(EntityUid uid, GrabbedComponent component, ComponentShutdown args)
    {
        _actionBlocker.UpdateCanMove(uid);

        // Cascade only if GrabbingComponent is not already shutting down — prevents double RemCompDeferred.
        // Running is true during Stopping, so check LifeStage directly.
        if (Exists(component.Grabber) &&
            TryComp<GrabbingComponent>(component.Grabber, out var grabbing) &&
            grabbing.LifeStage < ComponentLifeStage.Stopping &&
            grabbing.Victim == uid)
            RemCompDeferred<GrabbingComponent>(component.Grabber);
    }

    private void OnGrabbingShutdown(EntityUid uid, GrabbingComponent component, ComponentShutdown args)
    {
        // Stop dragging the victim if still pulling them.
        if (TryComp<PullableComponent>(component.Victim, out var pullable) && pullable.Puller == uid)
            _pulling.TryStopPull(component.Victim, pullable);

        // Deferred — this may fire while a GrabbedComponent event (e.g. MoveInputEvent) is active,
        // and immediate removal would corrupt the component storage iterator.
        if (TryComp<GrabbedComponent>(component.Victim, out var grabbed) && grabbed.Grabber == uid)
            RemCompDeferred<GrabbedComponent>(component.Victim);
    }
}
