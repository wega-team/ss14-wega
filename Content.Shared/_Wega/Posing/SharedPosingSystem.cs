using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, (float Angle, float OffsetX, float OffsetY, TimeSpan LastUpdate)> _continuousInput = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<PosingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<PosingComponent, MobStateChangedEvent>(OnMobStateChanged);

        BindCommands();
    }

    private void BindCommands()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, new TogglePosingCmdHandler(this))
            .Bind(ContentKeyFunctions.PosingOffsetRight, new ContinuousOffsetHandler(this, 0.2f, 0))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, new ContinuousOffsetHandler(this, -0.2f, 0))
            .Bind(ContentKeyFunctions.PosingOffsetUp, new ContinuousOffsetHandler(this, 0, 0.2f))
            .Bind(ContentKeyFunctions.PosingOffsetDown, new ContinuousOffsetHandler(this, 0, -0.2f))
            .Bind(ContentKeyFunctions.PosingRotatePositive, new ContinuousAngleHandler(this, -20f))
            .Bind(ContentKeyFunctions.PosingRotateNegative, new ContinuousAngleHandler(this, 20f))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedPosingSystem>();
        _continuousInput.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        foreach (var (uid, input) in _continuousInput.ToArray())
        {
            if (!TryComp<PosingComponent>(uid, out var posing) || !posing.Posing)
            {
                _continuousInput.Remove(uid);
                continue;
            }

            var timeSinceLastUpdate = now - input.LastUpdate;
            if (timeSinceLastUpdate.TotalSeconds < 0.016) // 60 FPS
                continue;

            var delta = Math.Min(0.1f, (float)timeSinceLastUpdate.TotalSeconds);

            if (input.Angle != 0)
                TryAdjustPosingAngle(uid, input.Angle * delta, posing);

            if (input.OffsetX != 0 || input.OffsetY != 0)
                TryAdjustPosingOffset(uid, new Vector2(input.OffsetX, input.OffsetY) * delta, posing);

            _continuousInput[uid] = (input.Angle, input.OffsetX, input.OffsetY, now);
        }
    }

    #region  Continuous Input
    private void StartContinuous(EntityUid uid, float angle = 0, float offsetX = 0, float offsetY = 0)
    {
        if (!HasComp<PosingComponent>(uid))
            return;

        _continuousInput[uid] = (angle, offsetX, offsetY, _timing.CurTime);
    }

    private void StopContinuous(EntityUid uid)
    {
        _continuousInput.Remove(uid);
    }
    #endregion

    #region Event Handlers
    private void OnDowned(Entity<PosingComponent> ent, ref DownedEvent args)
    {
        if (!ent.Comp.Posing)
            return;

        TogglePosing((ent.Owner, ent.Comp));
    }

    private void OnUpdateCanMove(Entity<PosingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Posing)
            args.Cancel();
    }

    private void OnMobStateChanged(Entity<PosingComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Posing)
            return;

        TogglePosing((ent.Owner, ent.Comp));
    }
    #endregion

    #region Adjustment Methods
    private void TryAdjustPosingOffset(EntityUid uid, Vector2 offset, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var newOffset = posingComp.CurrentOffset + offset;
        newOffset = Vector2.Clamp(newOffset, -posingComp.OffsetLimits, posingComp.OffsetLimits);

        if (Vector2.Distance(posingComp.CurrentOffset, newOffset) > 0.0001f)
        {
            posingComp.CurrentOffset = newOffset;
            Dirty(uid, posingComp);
        }
    }

    private void TryAdjustPosingAngle(EntityUid uid, float angle, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var newAngle = posingComp.CurrentAngle.Degrees + angle;
        var clampedAngle = Math.Clamp(newAngle, -posingComp.AngleLimits, posingComp.AngleLimits);

        if (Math.Abs(posingComp.CurrentAngle.Degrees - clampedAngle) > 0.01f)
        {
            posingComp.CurrentAngle = Angle.FromDegrees(clampedAngle);
            Dirty(uid, posingComp);
        }
    }
    #endregion

    #region Core Logic
    protected virtual void TogglePosing(Entity<PosingComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Posing = !entity.Comp.Posing;
        _actionBlocker.UpdateCanMove(entity.Owner);

        if (entity.Comp.Posing)
        {
            entity.Comp.CurrentOffset = Vector2.Zero;
            entity.Comp.CurrentAngle = Angle.Zero;
        }
        else
        {
            _continuousInput.Remove(entity.Owner);
        }

        Dirty(entity);
    }

    private bool CanTogglePosing(EntityUid uid)
    {
        if (!_mobState.IsAlive(uid))
            return false;

        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            return false;

        if (TryComp<StaminaComponent>(uid, out var stamina) && stamina.Critical)
            return false;

        if (HasComp<StunnedComponent>(uid))
            return false;

        if (_standing.IsDown(uid))
            return false;

        return true;
    }
    #endregion

    #region Command Handlers
    private sealed class TogglePosingCmdHandler : InputCmdHandler
    {
        private readonly SharedPosingSystem _system;
        public TogglePosingCmdHandler(SharedPosingSystem system) => _system = system;

        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid || message.State != BoundKeyState.Down)
                return false;

            if (_system.CanTogglePosing(uid))
                _system.TogglePosing(uid);

            return false;
        }
    }

    private sealed class ContinuousOffsetHandler : InputCmdHandler
    {
        private readonly SharedPosingSystem _system;
        private readonly float _offsetX;
        private readonly float _offsetY;

        public ContinuousOffsetHandler(SharedPosingSystem system, float offsetX, float offsetY)
        {
            _system = system;
            _offsetX = offsetX;
            _offsetY = offsetY;
        }

        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid)
                return false;

            if (message.State == BoundKeyState.Down)
                _system.StartContinuous(uid, offsetX: _offsetX, offsetY: _offsetY);
            else if (message.State == BoundKeyState.Up)
                _system.StopContinuous(uid);

            return false;
        }
    }

    private sealed class ContinuousAngleHandler : InputCmdHandler
    {
        private readonly SharedPosingSystem _system;
        private readonly float _angle;

        public ContinuousAngleHandler(SharedPosingSystem system, float angle)
        {
            _system = system;
            _angle = angle;
        }

        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid)
                return false;

            if (message.State == BoundKeyState.Down)
                _system.StartContinuous(uid, angle: _angle);
            else if (message.State == BoundKeyState.Up)
                _system.StopContinuous(uid);

            return false;
        }
    }
    #endregion
}
